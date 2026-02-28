---
name: execute-plans-parallel
description: Executes multiple independent implementation plans in parallel using subagents while preserving build, tests, and documentation integrity.
---

# Purpose

This skill orchestrates the parallel execution of multiple plan-*.prompt.md files by spawning subagents. It provides strict LLM-friendly prompts for each implementation phase to ensure a rigid quality gate.

Every completed implementation **MUST** satisfy all the following criteria before being considered done:
- Have an integration test covering the new code.
- Be code reviewed (self-reviewed against the original plan).
- Be documented under DocFX (XML comments + markdown files).
- Pass the solution build (dotnet build).
- Pass the full integration tests (dotnet test).

---

# When To Use

Use this skill when:
- Multiple plan files are ready for implementation.
- Plans are independent or partially independent.
- You want deterministic, CI-safe execution with strict quality enforcement.

---

# Execution Steps & Subagent Prompts

Follow this step-by-step workflow. In each step, you must pass the precise prompt to the subagent handling the task.

### Step 1: Pre-execution Dependency Analysis

Before making any changes, analyze all plans to avoid race conditions.

**Prompt to Subagent:** 
"Analyze the targeted plan-*.prompt.md files. Identify overlap by checking which code files, shared infrastructure, or public APIs they intend to modify. Output your analysis as a structured list dividing the plans into independentPlans (can run simultaneously) and sequentialPlans (must run one by one to avoid conflicts)."

### Step 2: Implementation & Code Quality

For each independent plan, spawn a parallel context and instruct the subagent to perform the actual coding incrementally.

**Prompt to Subagent:** 
"You are assigned to implement plan-[PlanName].prompt.md. Follow these mandatory directives:
1. Implement the functional changes specified in the plan incrementally.
2. Every new feature or change MUST be accompanied by an **Integration Test**. Create or update the integration tests appropriately.
3. Add full **DocFX** compliant documentation (update DocFX markdown files, and XML comments on APIs).
4. Preserve architectural layering and do no massive out-of-scope refactoring.
Do not run compilation or tests yet; just finalize the code."

### Step 3: Self-Review & Quality Gate Checklist

Once the subagent indicates the code is written, force a review.

**Prompt to Subagent:** 
"Before validation, conduct a strict code review of your own changes for plan-[PlanName].prompt.md:
- Checklist Item 1: Is there a solid integration test covering this exact feature?
- Checklist Item 2: Has the code been comprehensively documented according to DocFX standards?
- Checklist Item 3: Does the code adhere perfectly to the architecture specified in the plan?
If any item is missing or incomplete, output a warning and fix it immediately before proceeding."

### Step 4: Build & Test Validation

After the code review is clean, validate the codebase.

**Prompt to Subagent:** 
"Your code is ready for validation. You must now run the following terminal commands sequentially:
1. dotnet build - The build must pass unconditionally.
2. dotnet test - All unit and integration tests must pass unconditionally.
3. Check DocFX build locally to verify no fatal missing link errors for your API.
If any of these commands fail, identify the root cause of the compilation or test breakdown, apply the precise fix, and re-run all validation steps."

---

# Dependency Rules

Plans are considered dependent if they:
- Modify the same file.
- Change shared infrastructure.
- Alter the same public API.
- Affect shared concurrency mechanisms.

If dependency detected:
- Move these plans to the sequential execution group.
- Execute one, apply the standard Validation Gate (Steps 3 & 4), then merge and execute the next.

---

# Conflict Resolution Strategy

If parallel subagents cause a merge conflict or integration failure:
1. Pause parallel execution.
2. Roll back the interfering plans.
3. Re-evaluate the merge strategy and serialize their implementation.
4. Apply the validation gate sequentially.

---

# Safety Rules

- No forced refactors outside the plan's scope.
- No large sweeping overarching edits.
- Maintain backward compatibility unless the plan dictates otherwise.
- **Never** leave the repository in a broken state.

---

# Required Final Output

Upon completion, generate a final execution report containing:
1. **Executed Plans:** List of successfully executed plans.
2. **Serialized Plans:** List of plans executed sequentially due to overlap.
3. **Skipped Plans:** Any aborted plans and the reason for skipping.
4. **Validation Status:** Confirmation that Build, DocFX, and Integration Tests status are PASS.
5. **Modified Files:** Summary of files changed per plan.
6. **Risk Summary:** Identification of any lingering risks.
7. **PR Strategy:** Recommended PR batching approach.

---

# Failure Policy

If a subagent repeatedly fails the Validation Gate (build or test failure):
- Abort only that subagent's plan.
- Revert the files modified by that plan to preserve a clean repository state.
- Document the exact compilation/test failure reason in the Final Output.
- Continue processing the remaining independent plans.
