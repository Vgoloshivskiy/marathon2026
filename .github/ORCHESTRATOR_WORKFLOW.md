# Backend Implementation Orchestrator Workflow

## Overview

The Backend Implementation Orchestrator is a specialized agent designed to coordinate the implementation of backend features for `restaurant-backend-app.sln`. It works by:

1. **Accepting requirements** from users
2. **Creating a detailed implementation plan** (`.md` file)
3. **Delegating tasks** to specialized agents:
   - **AWS Lambda Implementation Agent**: Implements Lambda functions and AWS integrations
   - **Unit Test Writing Agent**: Writes comprehensive unit tests
   - **Restaurant Backend Code Review Agent**: Reviews code quality and compliance

## How to Use the Orchestrator

### Step 1: Invoke the Orchestrator

Request the orchestrator agent for your backend feature:

```
"I need to implement a feature to handle guest check-in notifications. 
When a guest checks in, the system should notify the waiter and update 
the reservation status. The feature needs a new Lambda function and 
should integrate with SNS for notifications."
```

### Step 2: Orchestrator Analyzes Requirements

The orchestrator will:
- Ask clarifying questions if needed
- Identify affected services
- Determine Lambda function requirements
- Check for dependencies with existing services

### Step 3: Plan Document Created

The orchestrator creates: `IMPLEMENTATION_PLAN_[FEATURE_NAME].md`

Example sections:
- Executive Summary
- Architecture & Data Model
- AWS Integration Requirements
- Phased Implementation Tasks
- Deliverables Tracking Table
- Success Criteria

### Step 4: Task Delegation

The orchestrator then delegates to specialized agents in sequence:

#### Phase 1: Lambda Implementation
```
Agent: AWS Lambda Implementation Agent
Task: Implement CheckInNotificationHandler Lambda function
Details:
  - Event source: API Gateway (POST /reservations/{id}/check-in)
  - AWS services: SNS, DynamoDB
  - Input: Reservation ID, guest info
  - Output: CheckInNotificationResponse
  - Error handling: Invalid reservation, notification failures
```

The agent implements and returns the Lambda function code.

#### Phase 2: Unit Testing (after Lambda complete)
```
Agent: Unit Test Writing Agent
Task: Write unit tests for CheckInNotificationHandler
Artifacts:
  - Lambda implementation from Phase 1
Requirements:
  - Coverage: 85%+
  - Test scenarios:
    * Valid guest check-in
    * Invalid reservation ID
    * SNS notification failure
    * DynamoDB update failure
    * Concurrent check-ins
```

The agent writes comprehensive unit tests.

#### Phase 3: Code Review (after tests complete)
```
Agent: Restaurant Backend Code Review Agent
Task: Review CheckInNotificationHandler implementation and tests
Artifacts:
  - Lambda from Phase 1
  - Tests from Phase 2
Review Criteria:
  - Logic correctness
  - AWS Lambda best practices
  - Performance optimization
  - Test coverage adequacy
  - Security/authorization
  - Design pattern compliance
```

The agent reviews both implementation and tests, provides feedback.

### Step 5: Consolidated Deliverables

Orchestrator updates the plan document with:
- ✅ Completed artifacts with links
- 📋 Code review feedback
- 🎯 Integration status
- ⚠️ Action items or improvements needed

## Example: Reservation Waitlist Feature

### Requirement Input

```
Feature: Add automatic waitlist management for reservation denials

Requirements:
- When a reservation cannot be accommodated, add guest to waitlist
- Notify guest of waitlist position
- When a reservation is cancelled, move waitlist guests to available slots
- API endpoint: POST /reservations/{id}/cancel with auto-promotion
- Notify promoted guests via email

Timeline: 2 weeks
Priority: High
```

### Generated Plan Document

File: `IMPLEMENTATION_PLAN_RESERVATION_WAITLIST.md`

```markdown
# Implementation Plan: Reservation Waitlist Management

## Executive Summary
Automatic waitlist system to improve customer experience and maximize 
reservation utilization.

## Requirement Analysis

### User Story
"As a restaurant manager, I want guests to be automatically moved from 
the waitlist when a reservation is cancelled, so we maximize table usage 
and customer satisfaction."

### Acceptance Criteria
- [ ] ReservationWaitlistHandler Lambda created
- [ ] Waitlist logic: FIFO queue, auto-promote on cancellation
- [ ] Guest notifications sent (email/SNS)
- [ ] 85%+ unit test coverage
- [ ] Code review approval
- [ ] Integration with existing ReservationService

## Architecture

### Components
- **ReservationWaitlistHandler**: Lambda processing cancellations and promotions
- **WaitlistService**: Business logic for queue management
- **NotificationService**: Handles guest notifications

### Data Model
- WaitlistEntry: ReservationID, GuestID, Position, CreatedAt
- DynamoDB Table: Waitlist (ReservationID as PK)

### AWS Integration
- Services: DynamoDB (Waitlist table), SNS (notifications), SES (email)
- Event: ReservationCancelled (EventBridge)
- IAM: DynamoDB write, SNS publish, SES send email

## Implementation Tasks

### Phase 1: Lambda Implementation
- [ ] Create ReservationWaitlistHandler.cs
- [ ] Implement WaitlistService with queue logic
- [ ] Integrate with DynamoDB and SNS
- [ ] Handle edge cases (concurrent cancellations)
- [ ] Add CloudWatch logging

### Phase 2: Unit Testing
- [ ] Test waitlist promotion logic
- [ ] Test notification sending (mocked SNS)
- [ ] Test concurrent operations
- [ ] Test error scenarios
- [ ] Achieve 85%+ coverage

### Phase 3: Code Review & Integration
- [ ] Logic correctness review
- [ ] Performance optimization review
- [ ] AWS best practices compliance
- [ ] Test adequacy review
- [ ] Integration verification

## Deliverables

| Phase | Task | Artifact | Status |
|-------|------|----------|--------|
| 1 | Lambda Implementation | dnapp/lambdas/reservation-waitlist/ReservationWaitlistHandler.cs | Pending |
| 2 | Unit Tests | Tests/Services/ReservationWaitlistServiceTests.cs | Pending |
| 3 | Code Review | REVIEW_FEEDBACK_RESERVATION_WAITLIST.md | Pending |

## Success Metrics
- Zero production issues in first month
- Average waitlist conversion rate: >40%
- Guest notification delivery: >99%
```

### Delegation Sequence

**Task 1 - Lambda Implementation (AWS Lambda Agent)**
```
Create ReservationWaitlistHandler Lambda with:
- Input: ReservationCancelledEvent (from EventBridge)
- Logic: Promote top waitlist guest, send notification
- Output: CancellationProcessedResponse
- AWS Services: DynamoDB, SNS, EventBridge
- Error handling: DynamoDB failures, SNS failures
```
*Result: ReservationWaitlistHandler.cs implemented*

**Task 2 - Unit Testing (Unit Test Agent)** *(waits for Task 1)*
```
Write comprehensive tests for ReservationWaitlistHandler:
- Test valid cancellation and promotion flow
- Test multiple waitlist guests ordering
- Test notification sending (mock SNS)
- Test DynamoDB failures and retries
- Test edge cases: concurrent cancellations, empty waitlist
- Target: 85%+ coverage
```
*Result: ReservationWaitlistServiceTests.cs with full test suite*

**Task 3 - Code Review (Code Review Agent)** *(waits for Tasks 1 & 2)*
```
Review and provide feedback on:
- Lambda implementation for logic correctness
- Unit tests for coverage and quality
- AWS best practices compliance
- Performance optimization opportunities
- Security and authorization checks
- Integration readiness
```
*Result: Code review feedback and checklist*

### Integration Complete

Plan document updated with:
- ✅ Phase 1: Lambda implementation complete (artifact linked)
- ✅ Phase 2: Unit tests complete, 88% coverage achieved (artifact linked)
- ✅ Phase 3: Code review complete, 2 minor improvements suggested
- 📝 Next: Deploy to staging environment for integration testing

## Key Benefits

### 1. Structured Workflow
- Clear phases: Plan → Implement → Test → Review
- Each phase builds on previous results
- Prevents rework and misalignment

### 2. Specialized Expertise
- AWS Lambda Agent knows serverless patterns
- Test Agent knows testing best practices
- Review Agent knows code quality standards
- Each agent focuses on their strength

### 3. Comprehensive Documentation
- Plan documents serve as reference
- Traceability between requirements and code
- Facilitates onboarding and knowledge transfer

### 4. Quality Assurance
- Code reviewed by dedicated agent
- Tests written for comprehensive coverage
- Lambda best practices enforced
- Multiple checkpoints before integration

## Plan Document Location & Structure

All implementation plans are saved in the project root:

```
backend/restaurant-backend-app/IMPLEMENTATION_PLAN_[FEATURE_NAME].md
```

### Naming Convention
- Replace `[FEATURE_NAME]` with kebab-case feature name
- Example: `IMPLEMENTATION_PLAN_RESERVATION_WAITLIST.md`
- Example: `IMPLEMENTATION_PLAN_GUEST_NOTIFICATIONS.md`

### Standard Sections
1. **Executive Summary** - High-level overview
2. **Requirement Analysis** - User story, acceptance criteria
3. **Architecture** - Components, data model, AWS integration
4. **Implementation Tasks** - Phased breakdown
5. **Deliverables** - Tracking table with artifact links
6. **Success Metrics** - Quantifiable outcomes

### Status Tracking
Use checkboxes to track progress:
```markdown
## Implementation Tasks

### Phase 1: Lambda Implementation
- [x] Create ReservationWaitlistHandler.cs
- [x] Implement WaitlistService with queue logic
- [ ] Integrate with DynamoDB and SNS
- [ ] Handle edge cases (concurrent cancellations)
- [ ] Add CloudWatch logging
```

Update the plan document as tasks complete.

## Common Patterns

### Pattern: New API Endpoint with Lambda + Database

```markdown
## Architecture
- **API Gateway** → **Lambda Handler** → **DynamoDB** + **External Service**
- Example: POST /guests/{id}/messages → GuestMessageHandler → Guest Messages table + SNS

## AWS Integration
- Event Source: API Gateway (HTTP POST)
- Data Store: DynamoDB (Guest Messages table)
- Notification: SNS (message routing)
- IAM: DynamoDB write, SNS publish permissions
```

### Pattern: Event-Driven Processing

```markdown
## Architecture
- **Event Source** → **Lambda Handler** → **Services** + **Notifications**
- Example: Reservation.Cancelled (EventBridge) → WaitlistPromoter → Waitlist table + Email/SNS

## AWS Integration
- Event Source: EventBridge rule
- Data Store: DynamoDB (Waitlist table)
- Notifications: SNS + SES (email)
- IAM: DynamoDB write, SNS/SES publish permissions
```

## Tips for Best Results

### Before Using Orchestrator

✅ **Do**:
- Have clear feature requirements documented
- Understand which services are affected
- Know if new Lambda or modifying existing
- Identify AWS services needed
- Have business context and success metrics

❌ **Don't**:
- Submit vague requirements ("add stuff")
- Skip architectural thinking
- Forget about dependencies
- Ignore data model design
- Assume no testing is needed

### After Getting Results

✅ **Do**:
- Review all plan documents thoroughly
- Link artifacts in tracking table
- Keep plan updated as work progresses
- Share plan with team for alignment
- Use plan as integration checklist

❌ **Don't**:
- Ignore code review feedback
- Deploy without completing all phases
- Lose track of plan documents
- Skip integration testing
- Forget to update team on progress

## Troubleshooting

### Agent Returns Incomplete Work
- Provide more specific requirements
- Link to relevant existing code as examples
- Break down tasks into smaller components
- Clarify acceptance criteria

### Plan Seems Too Large
- Split into smaller features
- Prioritize core functionality first
- Defer enhancements for phase 2
- Reduce scope if timeline is tight

### Code Review Finds Major Issues
- This is expected - review catches problems early
- Use findings to improve implementation
- No need to rework everything - address issues systematically
- This saves time vs. production issues

## Next Steps

1. **Prepare your feature requirements**
   - Business context
   - API contracts (if applicable)
   - AWS services needed
   - Dependencies

2. **Invoke the Orchestrator**
   - Use agent selection tool or `/subagents`
   - Select "Backend Implementation Orchestrator"
   - Provide requirements

3. **Review Generated Plan**
   - Understand architecture
   - Verify acceptance criteria
   - Note deliverables and timeline

4. **Monitor Progress**
   - Check artifacts as they're completed
   - Review feedback from each agent
   - Update plan document with status

5. **Integrate and Deploy**
   - Once all phases complete
   - Run integration tests
   - Deploy to production

---

**Ready to implement a backend feature?** Provide your requirements to the Backend Implementation Orchestrator agent and get a comprehensive implementation plan with coordinated specialist agents!
