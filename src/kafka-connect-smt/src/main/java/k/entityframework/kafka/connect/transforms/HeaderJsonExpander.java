package k.entityframework.kafka.connect.transforms;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.apache.kafka.common.config.AbstractConfig;
import org.apache.kafka.common.config.ConfigDef;
import org.apache.kafka.connect.connector.ConnectRecord;
import org.apache.kafka.connect.header.Header;
import org.apache.kafka.connect.header.Headers;
import org.apache.kafka.connect.transforms.Transformation;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Kafka Connect Single Message Transform (SMT) that reads a JSON-encoded
 * Kafka header blob and promotes selected fields to individual Kafka headers.
 *
 * <p>Designed for the K-Entity-Framework Debezium outbox pattern where the
 * {@code Headers} JSONB column is mapped to a single Kafka header
 * ({@code __debezium.outbox.headers}) by Debezium's EventRouter.
 * This SMT then extracts framework-required keys ({@code $type},
 * {@code $runtimeType}) into their own headers so the consumer and
 * type-routing logic can read them directly — identical to the polling
 * worker path.</p>
 *
 * <h3>Example connector config (chained after the EventRouter):</h3>
 * <pre>{@code
 * "transforms": "outbox,expandHeaders",
 * "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
 * ...
 * "transforms.outbox.table.fields.additional.placement": "Headers:header:__debezium.outbox.headers",
 * "transforms.expandHeaders.type": "k.entityframework.kafka.connect.transforms.HeaderJsonExpander",
 * "transforms.expandHeaders.source.header.name": "__debezium.outbox.headers",
 * "transforms.expandHeaders.extract.fields": "$type,$runtimeType"
 * }</pre>
 *
 * @param <R> the Connect record type
 */
public class HeaderJsonExpander<R extends ConnectRecord<R>> implements Transformation<R> {

    private static final Logger LOG = LoggerFactory.getLogger(HeaderJsonExpander.class);

    // ── Config keys ──────────────────────────────────────────────────────

    /** Name of the Kafka header that contains the JSON blob. */
    public static final String SOURCE_HEADER_NAME_CONFIG = "source.header.name";

    /** Whether to remove the source blob header after expansion. Default: true. */
    public static final String REMOVE_SOURCE_HEADER_CONFIG = "remove.source.header";

    // ── Defaults ─────────────────────────────────────────────────────────

    private static final String SOURCE_HEADER_NAME_DEFAULT = "__debezium.outbox.headers";

    private static final boolean REMOVE_SOURCE_HEADER_DEFAULT = true;

    private static final ConfigDef CONFIG_DEF = new ConfigDef()
            .define(
                    SOURCE_HEADER_NAME_CONFIG,
                    ConfigDef.Type.STRING,
                    SOURCE_HEADER_NAME_DEFAULT,
                    ConfigDef.Importance.HIGH,
                    "The name of the Kafka header containing the JSON blob to expand.")
            .define(
                    REMOVE_SOURCE_HEADER_CONFIG,
                    ConfigDef.Type.BOOLEAN,
                    REMOVE_SOURCE_HEADER_DEFAULT,
                    ConfigDef.Importance.MEDIUM,
                    "Whether to remove the source blob header after extracting its fields. Default: true.");

    // ── State ────────────────────────────────────────────────────────────

    private String sourceHeaderName;
    private boolean removeSourceHeader;
    private final ObjectMapper mapper = new ObjectMapper();
    private static final TypeReference<Map<String, String>> MAP_TYPE = new TypeReference<>() {};

    // ── Lifecycle ────────────────────────────────────────────────────────

    @Override
    public void configure(Map<String, ?> configs) {
        AbstractConfig parsed = new AbstractConfig(CONFIG_DEF, configs);
        sourceHeaderName = parsed.getString(SOURCE_HEADER_NAME_CONFIG);
        removeSourceHeader = parsed.getBoolean(REMOVE_SOURCE_HEADER_CONFIG);
    }

    @Override
    public R apply(R record) {
        if (record == null) {
            return null;
        }

        Headers headers = record.headers();
        Header blobHeader = lastHeader(headers, sourceHeaderName);
        if (blobHeader == null) {
            return record;
        }

        try {
            String json = headerValueToString(blobHeader);
            if (json == null || json.isEmpty()) {
                return record;
            }

            Map<String, String> entries = mapper.readValue(json, MAP_TYPE);

            for (Map.Entry<String, String> entry : entries.entrySet()) {
                if (entry.getValue() != null) {
                    headers.addString(entry.getKey(), entry.getValue());
                }
            }

            if (removeSourceHeader) {
                headers.remove(sourceHeaderName);
            }
        } catch (Exception e) {
            LOG.warn("Failed to expand JSON header '{}': {}", sourceHeaderName, e.getMessage(), e);
        }

        return record;
    }

    @Override
    public ConfigDef config() {
        return CONFIG_DEF;
    }

    @Override
    public void close() {
        // no resources to release
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /**
     * Converts a Connect header value to a UTF-8 string.
     * Handles both {@code String} and {@code byte[]} representations.
     */
    private static String headerValueToString(Header header) {
        Object value = header.value();
        if (value instanceof String) {
            return (String) value;
        }
        if (value instanceof byte[]) {
            return new String((byte[]) value, StandardCharsets.UTF_8);
        }
        return value != null ? value.toString() : null;
    }

    /**
     * Returns the last header with the given key, or {@code null}.
     * Mirrors Kafka's {@code Headers.lastHeader()} behaviour for Connect headers.
     */
    private static Header lastHeader(Headers headers, String key) {
        Header last = null;
        for (Header h : headers) {
            if (key.equals(h.key())) {
                last = h;
            }
        }
        return last;
    }
}
