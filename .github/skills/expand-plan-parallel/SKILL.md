---
name: expand-plan-parallel
description: Takes a raw implementation plan and decomposes it into detailed, independently implementable sub-plans using parallel subagents.
---

# Purpose

This skill transforms a high-level or raw plan document into structured, production-ready sub-plans.

It decomposes the plan into logical steps and runs parallel subagents to expand each step into a detailed implementation plan.

This skill does NOT implement code.  
It only produces structured plan artifacts.

---

# When To Use

Use this skill when:

- A plan is too coarse or high-level
- Steps need decomposition
- Work must be parallelized
- You want independent PR-sized tasks
- You want measurable, CI-safe breakdowns

---

# Execution Model

1. Read the provided raw plan file.
2. Identify major steps or logical sections.
3. Normalize steps into atomic work units.
4. Spawn one parallel subagent per step.
5. Each subagent expands its assigned step into a full sub-plan.
6. Collect and summarize all generated sub-plans.

Parallelization is mandatory if steps are independent.

---

# Decomposition Rules

A step is considered independent if:

- It modifies a distinct module
- It affects a different layer
- It has no shared state mutation
- It can be tested independently

If steps are interdependent:

- Identify dependency order
- Still expand independently
- Add dependency notes in each sub-plan

---

# Subagent Responsibilities

Each subagent must:

1. Expand one step only.
2. Create exactly one sub-plan file.
3. Use structured technical format.
4. Avoid speculative changes.
5. Remain CI-safe and incremental.
6. Not modify source code.

---

# Sub-Plan Naming Convention

If input plan is:

plan-featureX.prompt.md

Sub-plans must be named:

plan-featureX-step-01-<short-name>.prompt.md  
plan-featureX-step-02-<short-name>.prompt.md  
plan-featureX-step-03-<short-name>.prompt.md  

---

# Required Structure Per Sub-Plan

Each generated sub-plan must include:

```
---
name: plan name
description: plan description.
model: model to use for subagent
tools: [agent, search, read, edit]
---
```

1. Step ID and Title
2. Original Step Context
3. Objective
4. Scope (In / Out)
5. Technical Design
6. Detailed Implementation Steps
7. Affected Files
8. Concurrency Implications (if applicable)
9. Backward Compatibility Considerations
10. Test Strategy
11. Documentation Impact
12. Validation Requirements (Build/Test/DocFX)
13. Risk Analysis
14. Rollback Strategy
15. Measurable Acceptance Criteria

---

# Validation Discipline

Each sub-plan must explicitly state:

- How to verify build remains green
- What tests must pass
- What new tests must be added
- Whether DocFX requires updates

No sub-plan may assume successful implementation of another step unless explicitly declared as dependent.

---

# Output Requirements

After all subagents complete:

Return:

1. List of generated sub-plan files
2. Dependency map (if any)
3. High-level sequencing recommendation
4. Risk overview
5. Suggested PR batching strategy

---

# Constraints

- Do NOT modify code.
- Do NOT implement fixes.
- Do NOT merge steps.
- Keep each sub-plan reviewable (< ~1 PR worth of work).
- Favor deterministic, testable changes.
- Avoid large refactors in a single step.

---

# Example Usage

User prompt:

"Use expand-plan-parallel on plan-REV-006-applyOutboxCoordinationStrategy.prompt.md"

The skill will:

- Decompose coordination strategy fix
- Spawn parallel subagents per logical area
- Generate structured sub-plan files
- Return sequencing and risk summary