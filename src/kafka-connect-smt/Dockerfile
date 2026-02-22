# ── Stage 1: Build the SMT fat JAR ────────────────────────────────────
FROM maven:3.9-eclipse-temurin-17-alpine AS build
WORKDIR /build
COPY pom.xml .
RUN mvn dependency:resolve -q
COPY src ./src
RUN mvn package -DskipTests -q

# ── Stage 2: Extend Debezium Connect with the custom SMT ─────────────
FROM quay.io/debezium/connect:latest
COPY --from=build \
     /build/target/header-json-expander-1.0.0.jar \
     /kafka/connect/header-json-expander/header-json-expander-1.0.0.jar
