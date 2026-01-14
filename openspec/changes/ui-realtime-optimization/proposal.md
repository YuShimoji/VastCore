# UI Realtime Optimization Proposal

## Overview
Optimize the RealtimeUpdateSystem to balance responsiveness and frame budget via throttling, batching, and dynamic adaptation.

## Goals
- Prevent stutter by capping work-per-frame
- Dynamically adjust throttle and batch size based on average frame time
- Keep UI feedback responsive for high-priority updates

## Acceptance Criteria
- Average FPS stabilizes near target under heavy UI updates
- Max updates per frame adapts up/down without oscillation
- No nested/invalid method structure; code compiles and passes linting

## Risks
- Over-aggressive throttling reduces responsiveness
- Overly frequent adjustments cause instability
