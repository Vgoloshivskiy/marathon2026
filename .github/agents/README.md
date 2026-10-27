# Backend Implementation Agents

This directory contains specialized agents for implementing and managing backend features in the **restaurant-backend-app.sln** project. Each agent brings deep expertise in a specific domain.

## Available Agents

### 1. 🎯 Backend Implementation Orchestrator

**File**: [`orchestrator-agent.md`](./orchestrator-agent.md)

**Purpose**: Coordinates backend feature implementation from requirements to deployment

**Responsibilities**:
- Accepts feature requirements from users
- Creates structured implementation plans (`.md` documents)
- Delegates tasks to specialized agents in the correct sequence
- Tracks deliverables and provides consolidated feedback

**Use When**: You have a new backend feature to implement and want coordinated, multi-phase development

**Workflow**:
1. User provides requirements
2. Orchestrator creates: `IMPLEMENTATION_PLAN_[FEATURE_NAME].md`
3. Delegates Phase 1: Lambda implementation
4. Delegates Phase 2: Unit testing (after Phase 1 completes)
5. Delegates Phase 3: Code review (after Phases 1 & 2 complete)
6. Consolidates results and provides integration checklist

**Example**: *"Implement guest check-in notifications with automatic waiter assignment"*

---

### 2. ⚡ AWS Lambda Implementation Agent

**File**: [`aws-lambda-agent.md`](./aws-lambda-agent.md)

**Purpose**: Implements AWS Lambda functions and serverless components

**Specializations**:
- .NET/C# Lambda handler development
- AWS SDK integration (DynamoDB, SNS, SQS, etc.)
- Event handling (API Gateway, EventBridge, SQS, etc.)
- Infrastructure-as-Code with Terraform
- Syndicate framework for deployment

**Responsibilities**:
- Create new Lambda functions in `dnapp/lambdas/`
- Implement event handlers with correct signatures
- Integrate with AWS services and databases
- Handle errors, retries, and edge cases
- Add CloudWatch logging
- Update Terraform configuration

**Use When**: You need to implement a Lambda function or serverless handler

**Example Task**:
```
Implement ReservationWaitlistHandler Lambda function
- Event source: ReservationCancelled (EventBridge)
- Logic: Promote next guest from waitlist
- Integration: DynamoDB (Waitlist table) + SNS (notifications)
```

**Success Criteria**:
- ✅ Code compiles without errors
- ✅ Handles all event types correctly
- ✅ AWS service integrations functional
- ✅ Follows project patterns and conventions
- ✅ Ready for unit testing

---

### 3. 🧪 Unit Test Writing Agent

**File**: [`unit-test-agent.md`](./unit-test-agent.md)

**Purpose**: Writes comprehensive unit test suites

**Specializations**:
- xUnit 2.5.3 test framework
- Moq 4.20.72 for mocking
- AutoFixture 4.18.1 for test data
- Test coverage analysis
- AWS SDK mocking strategies

**Responsibilities**:
- Write unit tests for implemented features
- Achieve 80%+ code coverage
- Test happy paths, edge cases, and error scenarios
- Mock AWS service interactions
- Verify data transformations
- Test concurrency and error handling

**Use When**: You need comprehensive unit test coverage for a Lambda function or service

**Example Task**:
```
Write unit tests for WaiterAssignmentService
- Happy path: Assign available waiter
- Edge cases: No waiters available, concurrent assignments
- Error scenarios: DynamoDB failures, retry logic
- Coverage target: 85%+
```

**Success Criteria**:
- ✅ Coverage target achieved (85%+)
- ✅ All scenarios tested (happy, edge, error paths)
- ✅ Tests are meaningful and maintainable
- ✅ AWS services properly mocked
- ✅ Tests pass consistently

---

### 4. 👀 Restaurant Backend Code Review Agent

**File**: [`code-review-agent.md`](./code-review-agent.md)

**Purpose**: Reviews code quality and compliance with best practices

**Specializations**:
- Logic correctness verification
- Performance optimization
- AWS Lambda best practices
- .NET/C# design patterns
- Security and authorization checks
- Test adequacy assessment

**Responsibilities**:
- Review Lambda implementations for correctness
- Verify AWS service integrations follow best practices
- Assess code performance and scalability
- Check design patterns and code quality
- Evaluate test coverage and quality
- Provide actionable feedback

**Use When**: You need expert code review of implementation and tests

**Example Task**:
```
Review GuestCheckInHandler implementation and tests
- Verify logic correctness
- Check AWS best practices compliance
- Assess performance optimization
- Verify test coverage (target: 85%+)
- Provide integration readiness assessment
```

**Success Criteria**:
- ✅ All critical issues identified
- ✅ Feedback is actionable
- ✅ Integration readiness assessed
- ✅ Improvements documented
- ✅ Code approved for merge

---

## Workflow: From Concept to Production

### End-to-End Process

```
User Requirement
    ↓
Orchestrator Agent
  ├─ Analyze requirements
  └─ Create IMPLEMENTATION_PLAN_[FEATURE].md
    ↓
Phase 1: Lambda Implementation
  ├─ AWS Lambda Implementation Agent
  └─ Produces: Lambda function code
    ↓
Phase 2: Unit Testing
  ├─ Unit Test Writing Agent (waits for Phase 1)
  └─ Produces: Comprehensive test suite
    ↓
Phase 3: Code Review
  ├─ Code Review Agent (waits for Phases 1 & 2)
  └─ Produces: Review feedback & approval
    ↓
Orchestrator Consolidation
  ├─ Updates IMPLEMENTATION_PLAN with completion status
  ├─ Links all artifacts
  └─ Provides integration checklist
    ↓
Integration & Deployment Ready ✅
```

### Typical Timeline

| Phase | Agent | Duration | Dependencies |
|-------|-------|----------|--------------|
| Planning | Orchestrator | 1-2 hours | Requirements |
| Phase 1 | AWS Lambda Agent | 1-3 days | Plan document |
| Phase 2 | Unit Test Agent | 1-2 days | Phase 1 complete |
| Phase 3 | Code Review Agent | 0.5-1 day | Phases 1 & 2 complete |
| **Total** | **Orchestrator** | **1-2 weeks** | — |

---

## Getting Started

### 1. Request the Orchestrator Agent

When you have a backend feature to implement, select the **Backend Implementation Orchestrator** agent.

### 2. Provide Feature Requirements

Share:
- Feature description and business context
- API endpoints or event types
- Data models and AWS services needed
- Acceptance criteria and success metrics
- Any constraints or dependencies

**Example**:
```
I need to implement guest notifications when their 
reservation is ready for seating. This should:
- Listen to reservation state changes
- Send SMS and email notifications
- Track notification delivery status
- Handle delivery failures with retry logic

AWS Services: SNS for SMS/email, DynamoDB for tracking
Timeline: 1 week
Priority: High - improves customer experience
```

### 3. Review Generated Plan

The orchestrator creates: **`IMPLEMENTATION_PLAN_[FEATURE_NAME].md`**

Review the plan thoroughly:
- ✅ Verify acceptance criteria
- ✅ Confirm architecture understanding
- ✅ Check AWS service integrations
- ✅ Review task breakdown

### 4. Follow Phase Completion

Tasks delegate automatically to specialized agents:

**Phase 1**: AWS Lambda Agent implements  
**Phase 2**: Unit Test Agent writes tests (when Phase 1 ready)  
**Phase 3**: Code Review Agent reviews (when Phases 1 & 2 ready)

### 5. Track Progress

Plan document updates with:
- ✅ Completed artifacts and file paths
- 📋 Code review feedback
- 🎯 Integration status
- ⚠️ Action items

---

## Documentation

### For Users

- **[USING_ORCHESTRATOR.md](../USING_ORCHESTRATOR.md)**: Quick-start guide, examples, FAQ
- **[ORCHESTRATOR_WORKFLOW.md](../ORCHESTRATOR_WORKFLOW.md)**: Detailed workflow guide with patterns

### For Implementation

- **[IMPLEMENTATION_PLAN_TEMPLATE.md](../../backend/restaurant-backend-app/IMPLEMENTATION_PLAN_TEMPLATE.md)**: Template for creating plans
- **Individual Agent Files**: Each agent has detailed documentation in its `.md` file

### Project References

- **[restaurant-backend-app README](../../backend/restaurant-backend-app/README.md)**: Project structure
- **[API Routes](../../backend/restaurant-backend-app/docs/ApiRoutes.md)**: API specifications
- **[Local Setup Guide](../../backend/restaurant-backend-app/local-setup-guide.md)**: Development environment
- **[Deployment Resources](../../backend/restaurant-backend-app/deployment_resources.json)**: Infrastructure config

---

## Common Patterns

### Pattern: New API Endpoint

```
Orchestrator Plan
├─ Phase 1: Implement APIHandler Lambda for new endpoint
├─ Phase 2: Write tests for request validation & response
└─ Phase 3: Review API integration & error handling

Example: POST /guests/{id}/notifications
```

### Pattern: Event-Driven Processing

```
Orchestrator Plan
├─ Phase 1: Implement event processor Lambda
├─ Phase 2: Write tests for event handling & edge cases
└─ Phase 3: Review EventBridge integration & scalability

Example: Process ReservationCancelled events
```

### Pattern: Background Job

```
Orchestrator Plan
├─ Phase 1: Implement scheduled Lambda (CloudWatch Events)
├─ Phase 2: Write tests for job execution & error recovery
└─ Phase 3: Review job reliability & performance

Example: Generate daily reports
```

---

## Best Practices

### ✅ DO

- Use the orchestrator for all backend features
- Create comprehensive implementation plans before coding
- Let each agent complete their phase before moving on
- Review and address code review feedback
- Link artifacts in the plan document
- Use plan documents as team knowledge base
- Keep plans updated as work progresses

### ❌ DON'T

- Skip the planning phase
- Delegate incomplete requirements
- Skip any implementation phases
- Ignore code review feedback
- Leave plan documents in inconsistent states
- Deploy without completing all phases
- Forget to update Terraform for new Lambda functions

---

## Troubleshooting

### Agent Returns Incomplete Work

**Problem**: Lambda implementation doesn't compile or is missing pieces

**Solution**:
1. Provide more specific requirements
2. Link to existing code as examples
3. Break down tasks into smaller components
4. Clarify acceptance criteria and error scenarios

### Code Review Finds Major Issues

**Problem**: Phase 3 review reveals fundamental problems

**Solution**:
1. This is expected - review catches problems early
2. Address critical issues systematically
3. Use feedback to improve implementation
4. Saves time vs. finding bugs in production

### Tests Have Low Coverage

**Problem**: Phase 2 doesn't achieve coverage target

**Solution**:
1. Identify uncovered code paths
2. Ask Unit Test Agent to add specific test scenarios
3. Review for testable design (excess coupling)
4. Add integration tests if needed

---

## Support

### Documentation

- 📚 [Using Orchestrator Guide](../USING_ORCHESTRATOR.md)
- 📚 [Orchestrator Workflow](../ORCHESTRATOR_WORKFLOW.md)
- 📚 [Implementation Plan Template](../../backend/restaurant-backend-app/IMPLEMENTATION_PLAN_TEMPLATE.md)

### Individual Agent Docs

- 📖 [AWS Lambda Agent](./aws-lambda-agent.md)
- 📖 [Unit Test Agent](./unit-test-agent.md)
- 📖 [Code Review Agent](./code-review-agent.md)
- 📖 [Orchestrator Agent](./orchestrator-agent.md)

### Project Resources

- 📋 [API Routes Documentation](../../backend/restaurant-backend-app/docs/ApiRoutes.md)
- 🏗️ [Architecture Diagrams](../../backend/restaurant-backend-app/docs/Diagrams.md)
- 🛠️ [Local Setup Guide](../../backend/restaurant-backend-app/local-setup-guide.md)

---

## Quick Reference

### Invoking Agents

| Agent | When to Use | Deliverable |
|-------|------------|------------|
| **Orchestrator** | New feature to implement | `IMPLEMENTATION_PLAN_[FEATURE].md` |
| **AWS Lambda** | Lambda function needed | `.cs` implementation files |
| **Unit Test** | Test coverage required | `*Tests.cs` with test suite |
| **Code Review** | Quality assurance needed | Review feedback & checklist |

### File Locations

```
.github/
├─ agents/
│  ├─ orchestrator-agent.md         ← START HERE
│  ├─ aws-lambda-agent.md
│  ├─ unit-test-agent.md
│  ├─ code-review-agent.md
│  └─ README.md                     ← You are here
├─ USING_ORCHESTRATOR.md             ← Quick start guide
├─ ORCHESTRATOR_WORKFLOW.md          ← Detailed workflow

backend/restaurant-backend-app/
├─ IMPLEMENTATION_PLAN_TEMPLATE.md   ← Plan template
├─ dnapp/lambdas/                    ← Lambda functions
├─ Tests/                            ← Unit tests
└─ docs/                             ← Architecture docs
```

---

## Next Steps

1. **Read** [USING_ORCHESTRATOR.md](../USING_ORCHESTRATOR.md) for quick start
2. **Prepare** your feature requirements
3. **Invoke** the Backend Implementation Orchestrator agent
4. **Follow** the 3-phase workflow to completion
5. **Review** the generated implementation plan and artifacts
6. **Integrate** code into main branch

---

**Ready to implement a backend feature?**  
Start with the **Backend Implementation Orchestrator** agent! 🚀
