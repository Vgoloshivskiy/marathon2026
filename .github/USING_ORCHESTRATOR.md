# Using the Backend Implementation Orchestrator

## Quick Start

### 1. Request the Orchestrator Agent

When you have a backend feature to implement, use the `Backend Implementation Orchestrator` agent.

In VS Code Copilot, you can:
- Say: "I want to use the Backend Implementation Orchestrator"
- Or: Select the agent from the agent picker dropdown

### 2. Provide Your Feature Requirements

Give the orchestrator:
- **Feature description**: What does it do?
- **Business context**: Why is it needed?
- **API/Event**: Request/response format or event type
- **AWS services**: DynamoDB, SNS, SQS, etc.
- **Dependencies**: Related services or data
- **Constraints**: Performance, security, timeline

**Example**:
```
I need to implement guest check-in notifications. When a guest 
checks in through the API, the system should:
1. Update the reservation status to "checked_in"
2. Send a notification to the assigned waiter (via SNS)
3. Log the check-in event to CloudWatch

This should be a new Lambda function that integrates with:
- DynamoDB (Reservations table)
- SNS (Waiter notifications)
- EventBridge (for downstream processing)

I'd like this ready in 1 week. Main concern is notification 
reliability - no guest should be left unnotified.
```

### 3. Review the Implementation Plan

The orchestrator will create: **`IMPLEMENTATION_PLAN_[FEATURE_NAME].md`**

Located in: `backend/restaurant-backend-app/`

This plan contains:
- ✅ Acceptance criteria
- 🏗️ Architecture design
- 📋 Phased implementation tasks
- 🎯 Deliverables tracking
- 📊 Success metrics

**Review the plan thoroughly** - it's your blueprint!

### 4. Task Delegation Begins

The orchestrator delegates work to specialized agents:

#### Phase 1: Implementation
**Agent**: AWS Lambda Implementation Agent
```
Creates: Lambda function (.cs file)
Example: CheckInNotificationHandler.cs
Time: ~1-2 days per function
```

#### Phase 2: Testing  
**Agent**: Unit Test Writing Agent
```
Requires: Phase 1 completion
Creates: Unit test file (.cs with tests)
Example: CheckInNotificationHandlerTests.cs
Time: ~1-2 days for comprehensive tests
```

#### Phase 3: Review
**Agent**: Restaurant Backend Code Review Agent
```
Requires: Phases 1 & 2 completion
Creates: Code review feedback (.md document)
Checks: Logic, performance, best practices, security
Time: ~1 day for thorough review
```

### 5. Track Progress

The orchestrator updates the plan document with:
- ✅ Completed phases with artifact links
- 📝 Code review feedback
- 🔗 References to implementation and test files
- ⚠️ Any action items or improvements needed

**Status Table Example**:
```
| Phase | Task | Artifact | Status |
|-------|------|----------|--------|
| 1 | Lambda Implementation | dnapp/lambdas/check-in-handler/CheckInNotificationHandler.cs | ✅ Complete |
| 2 | Unit Tests | Tests/CheckInNotificationHandlerTests.cs | ✅ Complete (88% coverage) |
| 3 | Code Review | REVIEW_FEEDBACK_CHECK_IN.md | ✅ Complete - 2 notes |
```

### 6. Next Steps

Once complete:
1. Review all artifacts (implementation and tests)
2. Address any code review feedback
3. Run integration tests
4. Update Terraform configuration (if new Lambda)
5. Deploy to staging environment
6. Perform end-to-end testing
7. Merge to main branch and deploy

---

## What the Orchestrator Does

### Creates Implementation Plans

For each feature, generates: **`IMPLEMENTATION_PLAN_[FEATURE_NAME].md`**

**Example plan structure**:
```markdown
# Implementation Plan: Guest Check-In Notifications

## Executive Summary
Brief feature overview and business value

## Requirement Analysis  
User stories, acceptance criteria, success metrics

## Architecture
Component diagrams, data models, AWS services

## Implementation Tasks
Broken into 3 phases with specific, testable tasks

## Deliverables
Table tracking all artifacts and completion status
```

### Manages Task Dependencies

Ensures correct sequence:
```
Requirements Analysis
    ↓
Plan Creation
    ↓
AWS Lambda Implementation (Phase 1)
    ↓
Unit Test Writing (Phase 2) ← waits for Phase 1
    ↓
Code Review (Phase 3) ← waits for Phases 1 & 2
    ↓
Consolidated Feedback & Integration Ready
```

### Coordinates Specialized Agents

**Each agent brings deep expertise**:

| Agent | Specialization | Deliverable |
|-------|-----------------|-------------|
| AWS Lambda Implementation | Serverless, AWS SDK, event handling | Lambda function code |
| Unit Test Writing | xUnit, Moq, AutoFixture, coverage | Comprehensive test suite |
| Code Review | Logic, performance, best practices | Feedback & checklists |

### Tracks Artifacts

All outputs linked in plan document:
- 📄 Lambda implementation file (with line numbers)
- 📋 Test file and coverage reports
- 📝 Code review feedback and suggestions
- 🔗 Related documentation and references

---

## Example: Complete Workflow

### Step 1: Request Feature Implementation

**You**: "I need to add a feature to automatically assign waiters to new reservations."

### Step 2: Orchestrator Asks Clarifying Questions

**Orchestrator**: 
- Should waiters be assigned immediately on reservation creation or later?
- What's the selection logic (random, round-robin, skill-based)?
- Do waiters get notified? How (SNS, email)?
- What if no waiters are available?

### Step 3: Orchestrator Creates Plan

**Creates**: `IMPLEMENTATION_PLAN_WAITER_ASSIGNMENT.md`

Contains:
- ✅ Acceptance criteria: "All new reservations assigned waiter within 100ms"
- 🏗️ Architecture: ReservationHandler → AssignmentService → WaiterNotifier
- 📋 Tasks:
  - Phase 1: AssignmentHandler Lambda with round-robin logic
  - Phase 2: Unit tests for all assignment scenarios
  - Phase 3: Review for performance optimization

### Step 4: Phase 1 - AWS Lambda Agent

**Creates**: `dnapp/lambdas/waiter-assignment/AssignmentHandler.cs`

Code includes:
- Event handler for ReservationCreated events
- WaiterAssignmentService with assignment logic
- DynamoDB integration for waiter status
- SNS notification sending
- Error handling and logging

### Step 5: Phase 2 - Unit Test Agent

**Sees**: AssignmentHandler implementation
**Creates**: `Tests/WaiterAssignmentServiceTests.cs`

Tests cover:
- ✅ Happy path: Assign available waiter
- ✅ Edge cases: No waiters available, all on break
- ✅ Concurrent assignments (thread safety)
- ✅ DynamoDB failures and retry logic
- ✅ SNS notification delivery
- **Coverage**: 92% (exceeds 80% target)

### Step 6: Phase 3 - Code Review Agent

**Sees**: Implementation + Tests
**Creates**: `REVIEW_FEEDBACK_WAITER_ASSIGNMENT.md`

Review checks:
- ✅ Logic is correct and handles edge cases
- 💡 Performance: Suggest connection pooling for DynamoDB
- 💡 Scalability: Consider caching available waiters
- ✅ AWS best practices followed
- ✅ Test coverage is comprehensive

### Step 7: Consolidated Feedback

**Orchestrator updates plan**:
```
## Deliverables

| Phase | Task | Artifact | Status |
|-------|------|----------|--------|
| 1 | Lambda Implementation | dnapp/lambdas/waiter-assignment/AssignmentHandler.cs | ✅ Complete |
| 2 | Unit Tests | Tests/WaiterAssignmentServiceTests.cs | ✅ Complete (92% coverage) |
| 3 | Code Review | REVIEW_FEEDBACK_WAITER_ASSIGNMENT.md | ✅ Complete - 2 optimization suggestions |

## Next Steps
1. ✅ Review optimization suggestions (optional for MVP)
2. ✅ Deploy to staging
3. ✅ Run end-to-end tests
4. ✅ Merge to main
```

---

## Plan Document Locations

All plans saved to: `backend/restaurant-backend-app/`

**Examples**:
```
✅ IMPLEMENTATION_PLAN_WAITER_ASSIGNMENT.md
✅ IMPLEMENTATION_PLAN_RESERVATION_WAITLIST.md
✅ IMPLEMENTATION_PLAN_GUEST_CHECK_IN.md
✅ IMPLEMENTATION_PLAN_REPORT_SCHEDULING.md
```

Use these as:
- Reference during development
- Team alignment document
- Progress tracking sheet
- Integration checklist
- Knowledge base for future similar features

---

## Key Benefits

### 1. **Faster Implementation**
Clear plan prevents rework and misalignment. Parallel phases (once Lambda is ready, tests start immediately).

### 2. **Better Quality**
Three different perspectives (implementation, testing, review) catch issues early. High code quality before deployment.

### 3. **Clear Documentation**
Implementation plan serves as reference for team. Traceability from requirements to code.

### 4. **Consistent Process**
Every feature follows same structured workflow: Plan → Implement → Test → Review

### 5. **Knowledge Transfer**
Plan documents become team knowledge base. New team members can understand past features easily.

---

## FAQ

### Q: What if I don't know all the requirements upfront?

**A**: That's fine! The orchestrator will ask clarifying questions. Give what you know, and the orchestrator will help refine the requirements.

### Q: Can I skip any phases?

**A**: Not recommended. Each phase adds value:
- **Phase 1** (Implementation) - Creates working code
- **Phase 2** (Testing) - Ensures reliability and catches bugs
- **Phase 3** (Review) - Catches performance issues and design problems

Skipping leads to technical debt and production issues.

### Q: What if Phase 1 implementation has bugs?

**A**: The Unit Test Agent (Phase 2) will likely catch them. The Code Review Agent (Phase 3) will also find issues. This is why the multi-phase approach works - multiple checkpoints.

### Q: Can I use this for simple changes?

**A**: Yes! Even small features benefit from planning. The orchestrator can scale the plan to match complexity.

### Q: How long does a full workflow take?

**A**: Typical timeline:
- **Planning**: 1-2 hours
- **Phase 1 (Lambda)**: 1-3 days
- **Phase 2 (Tests)**: 1-2 days  
- **Phase 3 (Review)**: 0.5-1 day
- **Total**: 1-2 weeks depending on complexity

### Q: What if I disagree with code review feedback?

**A**: Discuss with the code review agent! Not all suggestions are mandatory. Focus on addressing critical issues first, defer optimizations if timeline is tight.

### Q: Can I modify the plan after it's created?

**A**: Absolutely! Plans are living documents. Update them as:
- Requirements clarify
- Technical constraints emerge  
- Implementation decisions are made
- Progress is tracked

---

## Getting Started

### Right Now:

1. ✅ You've read this guide
2. ✅ You know what the orchestrator does
3. ✅ You understand the 3-phase workflow

### Next:

1. 📝 Prepare your feature requirements
2. 🤖 Request the "Backend Implementation Orchestrator" agent
3. 📋 Provide your requirements to the agent
4. 📄 Review the generated implementation plan
5. ⏳ Wait for agent delegation and phase completion
6. ✅ Track progress and integrat

e code

---

**Ready to implement a backend feature with the orchestrator?**

Just provide your requirements to the **Backend Implementation Orchestrator** agent and get started!
