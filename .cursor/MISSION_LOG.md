# Mission Log

> 縺薙・繝輔ぃ繧､繝ｫ縺ｯ縲、I繧ｨ繝ｼ繧ｸ繧ｧ繝ｳ繝茨ｼ・rchestrator 縺ｨ Worker・峨・菴懈･ｭ險倬鹸繧堤ｮ｡逅・☆繧九◆繧√・SSOT縺ｧ縺吶・
> Orchestrator 縺ｨ Worker 縺ｯ縲√％縺ｮ繝輔ぃ繧､繝ｫ繧定ｪｭ縺ｿ譖ｸ縺阪＠縺ｦ縲√ち繧ｹ繧ｯ縺ｮ迥ｶ諷九ｒ蜷梧悄縺励∪縺吶・

---

## 蝓ｺ譛ｬ諠・ｱ

- **Mission ID**: ORCH_20260209_ROADMAP_PHASE_A
- **髢句ｧ区律譎・*: 2026-02-09T08:48:00+09:00
- **譛邨よ峩譁ｰ**: 2026-02-09T08:48:00+09:00
- **迴ｾ蝨ｨ縺ｮ繝輔ぉ繝ｼ繧ｺ**: Phase A - Stabilization
- **繧ｹ繝・・繧ｿ繧ｹ**: IN_PROGRESS

---

## 繝輔ぉ繝ｼ繧ｺ讎りｦ・

**Phase A: 螳牙ｮ壼喧 (Stabilization)** 窶・1-2 繧ｹ繝励Μ繝ｳ繝・
- **繧ｴ繝ｼ繝ｫ**: 繧ｳ繝ｳ繝代う繝ｫ螳牙ｮ壽ｧ 95縲∝・繝悶Ο繝・き繝ｼ隗｣豸医√ン繝ｫ繝峨′遒ｺ螳溘↓騾壹ｋ迥ｶ諷・
- **繧ｽ繝ｼ繧ｹ**: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md` (L170-249)

---

## Tier 蜑ｲ繧雁ｽ薙※

| 繧ｿ繧ｹ繧ｯID | 隱ｬ譏・| Tier | 繧ｵ繧､繧ｺ | 荳ｦ蛻怜ｮ溯｡・| 繝ｪ繧ｹ繧ｯ |
|---------|------|------|--------|---------|--------|
| PA-1 | Deform 繧ｹ繧ｿ繝匁紛逅・| Tier 1 | S | 蜿ｯ (Worker蜊倡峡) | 菴・|
| PA-2 | ProBuilder API 遘ｻ陦・| Tier 2 | L | 谿ｵ髫主ｮ溯｡・| 鬮・|
| PA-3 | asmdef 豁｣隕丞喧 | Tier 1 | S | 蜿ｯ (Worker蜊倡峡) | 菴・|
| PA-4 | 繝・せ繝医ヵ繧｡繧､繝ｫ謨ｴ逅・| Tier 1 | M | 蜿ｯ (Worker蜊倡峡) | 菴・|
| PA-5 | Unity Editor 繧ｳ繝ｳ繝代う繝ｫ讀懆ｨｼ | Tier 3 | S | 荳榊庄 (謇句虚讀懆ｨｼ蠢・・ | 荳ｭ |

---

## 繧ｿ繧ｹ繧ｯ萓晏ｭ倬未菫・

```
PA-1 笏笏笏ｬ笏笏竊・PC-1 (Blocks: Deform豁｣蠑丞ｰ主・)
       笏披楳笏竊・PA-2 (Depends-On: PA-1)
PA-2 笏笏竊・PC-2 (Blocks: CSG/Composition)
PA-3 笏笏笏ｬ笏笏竊・PB-5 (Blocks: Core蛻・牡)
       笏披楳笏竊・PA-5 (Depends-On: PA-1, PA-3, PA-4)
PA-4 笏笏笏ｬ笏笏竊・PB-1 (Blocks: 繝・せ繝亥渕逶､)
       笏披楳笏竊・PA-5 (Depends-On)
PA-5 笏笏笏竊・(蜈ｨ蠕檎ｶ壹ヵ繧ｧ繝ｼ繧ｺ縺ｮ繝悶Ο繝・き繝ｼ)
```

---

## 迴ｾ蝨ｨ縺ｮ繧ｿ繧ｹ繧ｯ

### 荳ｦ蛻怜ｮ溯｡御ｸｭ (Tier 1)

| 繧ｿ繧ｹ繧ｯID | 隱ｬ譏・| Tier | Status | Worker | 騾ｲ謐・|
|---------|------|------|--------|--------|------|
| PA-1 | Deform 繧ｹ繧ｿ繝匁紛逅・| Tier 1 | READY | - | 繝√こ繝・ヨ菴懈・蠕・■ |
| PA-3 | asmdef 豁｣隕丞喧 | Tier 1 | READY | - | 繝√こ繝・ヨ菴懈・蠕・■ |
| PA-4 | 繝・せ繝医ヵ繧｡繧､繝ｫ謨ｴ逅・| Tier 1 | READY | - | 繝√こ繝・ヨ菴懈・蠕・■ |

### 蠕・ｩ滉ｸｭ (Tier 2/3)

| 繧ｿ繧ｹ繧ｯID | 隱ｬ譏・| Tier | Status | Worker | 騾ｲ謐・|
|---------|------|------|--------|--------|------|
| PA-2 | ProBuilder API 遘ｻ陦・| Tier 2 | BLOCKED | - | PA-1 螳御ｺ・ｾ・■ |
| PA-5 | Unity Editor 讀懆ｨｼ | Tier 3 | BLOCKED | - | PA-1, PA-3, PA-4 螳御ｺ・ｾ・■ |

---

## Phase A 螳御ｺ・渕貅・

- [ ] 繧ｳ繝ｳ繝代う繝ｫ繧ｨ繝ｩ繝ｼ 0
- [ ] asmdef 萓晏ｭ倥′蜈ｨ縺ｦ譏守､ｺ逧・
- [ ] 繝・せ繝医ヵ繧｡繧､繝ｫ縺・Testing 繧｢繧ｻ繝ｳ繝悶Μ縺ｫ髮・ｴ・
- [ ] Deform 譚｡莉ｶ莉倥″繧ｳ繝ｳ繝代う繝ｫ縺檎ｵｱ荳
- [ ] 蛛･蜈ｨ諤ｧ繧ｹ繧ｳ繧｢: 繧ｳ繝ｳ繝代う繝ｫ螳牙ｮ壽ｧ 竊・95

---

## Forbidden Area 螳夂ｾｩ

### PA-1 螳溯｡御ｸｭ
- **邱ｨ髮・ｦ∵ｭ｢**: `Scripts/Generation/DeformIntegration.cs` 縺ｮ螳溯｣・Ο繧ｸ繝・け螟画峩
- **險ｱ蜿ｯ**: 譚｡莉ｶ莉倥″繧ｳ繝ｳ繝代う繝ｫ (`#if DEFORM_PACKAGE`) 縺ｮ霑ｽ蜉縺ｮ縺ｿ
- **險ｱ蜿ｯ**: `Scripts/Deform/DeformStubs.cs` 縺ｮ譁ｰ隕丈ｽ懈・繝ｻ邱ｨ髮・

### PA-2 螳溯｡御ｸｭ
- **邱ｨ髮・ｦ∵ｭ｢**: ProBuilder 繝代ャ繧ｱ繝ｼ繧ｸ閾ｪ菴薙・螟画峩
- **邱ｨ髮・ｦ∵ｭ｢**: 譌｢蟄倥・髱・ProBuilder 髢｢騾｣繧ｳ繝ｼ繝峨・螟画峩
- **險ｱ蜿ｯ**: `HighQualityPrimitiveGenerator.cs`, `PrimitiveTerrainGenerator.cs`, `PrimitiveModifier.cs` 縺ｮ API 遘ｻ陦・
- **險ｱ蜿ｯ**: `MeshSubdivider.cs` 縺ｮ譁ｰ隕丈ｽ懈・・医ヵ繧ｩ繝ｼ繝ｫ繝舌ャ繧ｯ螳溯｣・ｼ・

### PA-3 螳溯｡御ｸｭ
- **邱ｨ髮・ｦ∵ｭ｢**: 繧ｽ繝ｼ繧ｹ繧ｳ繝ｼ繝・(.cs) 縺ｮ繝ｭ繧ｸ繝・け螟画峩
- **險ｱ蜿ｯ**: `.asmdef` 繝輔ぃ繧､繝ｫ縺ｮ蜿ら・險ｭ螳壹・autoReferenced 螟画峩縺ｮ縺ｿ

### PA-4 螳溯｡御ｸｭ
- **邱ｨ髮・ｦ∵ｭ｢**: 繝・せ繝医ヵ繧｡繧､繝ｫ縺ｮ蜀・ｮｹ螟画峩・育ｧｻ蜍輔・縺ｿ・・
- **邱ｨ髮・ｦ∵ｭ｢**: 遘ｻ蜍募・繝輔ぃ繧､繝ｫ縺ｮ蜑企勁・亥盾辣ｧ譖ｴ譁ｰ螳御ｺ・∪縺ｧ・・
- **險ｱ蜿ｯ**: 繝輔ぃ繧､繝ｫ縺ｮ `Scripts/Testing/` 縺ｸ縺ｮ遘ｻ蜍・
- **險ｱ蜿ｯ**: `Vastcore.Testing.asmdef` 縺ｮ蜿ら・遽・峇譖ｴ譁ｰ

### PA-5 螳溯｡御ｸｭ
- **邱ｨ髮・ｦ∵ｭ｢**: 譁ｰ讖溯・霑ｽ蜉
- **險ｱ蜿ｯ**: 繧ｳ繝ｳ繝代う繝ｫ繧ｨ繝ｩ繝ｼ縺ｮ縺ｿ菫ｮ豁｣
- **險ｱ蜿ｯ**: 繝ｬ繝昴・繝井ｽ懈・

---

## Unity Editor 讀懆ｨｼ繝輔Ο繝ｼ

```
Worker (PA-5) 竊・Report: "Compile check required in Unity Editor"
  竊・
Orchestrator 竊・User: "Unity Editor縺ｧ繧ｳ繝ｳ繝代う繝ｫ遒ｺ隱阪ｒ螳滓命縺励※縺上□縺輔＞"
  竊・
User confirms 竊・Orchestrator updates TASK_PA-5 Status: VERIFIED
  竊・
Phase B 髢句ｧ・(PB-1)
```

---

## 谺｡縺ｮ繧｢繧ｯ繧ｷ繝ｧ繝ｳ

### 蜊ｳ蠎ｧ縺ｫ逹謇九☆縺ｹ縺阪％縺ｨ
1. PA-1, PA-3, PA-4 縺ｮ繝√こ繝・ヨ菴懈・ (`docs/tasks/TASK_PA-*.md`)
2. Worker Prompt 菴懈・ (`docs/inbox/WORKER_PROMPT_PA-*.md`)
3. 繝ｦ繝ｼ繧ｶ繝ｼ縺ｸ Phase A 髢句ｧ九・謇ｿ隱榊叙蠕・

### 谺｡蝗・Orchestrator 縺檎｢ｺ隱阪☆縺ｹ縺阪％縺ｨ
- [ ] PA-1, PA-3, PA-4 縺ｮ螳御ｺ・｢ｺ隱・
- [ ] PA-2 縺ｮ隱ｿ譟ｻ繝輔ぉ繝ｼ繧ｺ髢句ｧ・
- [ ] PA-5 縺ｮ Unity Editor 讀懆ｨｼ萓晞ｼ

---

## 螟画峩螻･豁ｴ

### `2026-02-09T08:48:00+09:00` - `Orchestrator` - `Mission Start`
- 譁ｰ隕上Α繝・す繝ｧ繝ｳ髢句ｧ・(ORCH_20260209_ROADMAP_PHASE_A)
- Phase A (Stabilization) 縺ｮ Tier 蜑ｲ繧雁ｽ薙※螳御ｺ・
- Forbidden Area 螳夂ｾｩ螳御ｺ・
- Unity Editor 讀懆ｨｼ繝輔Ο繝ｼ遒ｺ遶・

### `2026-02-03T23:40:00+09:00` - `Orchestrator` - `Fixed Compilation Ticket Created`
- Mission ID: ORCH_20260203_FIX_COMPILATION
- **Status**: P5 (Worker Delegation) Complete
- **Events**:
  - `TASK_028_FixPrimitiveTerrainCompilation.md` created to address `PrimitiveTerrainObject` compilation errors.
  - `WORKER_PROMPT_TASK_028.md` created for immediate execution.
  - `task.md` updated.
- **Next Action**: User to execute Worker with `docs/inbox/WORKER_PROMPT_TASK_028.md`.

### `2026-02-04T02:50:00+09:00` - `Worker` - `TASK_028 Completion`
- Mission ID: ORCH_20260203_FIX_COMPILATION
- **Status**: DONE
- **Results**:
  - `PrimitiveTerrainObject.cs` implements `IPoolable` members.
  - `VastcoreEditorRoot.cs` created in `Assets/Editor`.
  - Report: `docs/inbox/REPORT_TASK_028_FixPrimitiveTerrainCompilation.md`.
- **Verification**: Code review passed. Files exist and content matches requirements.
- **Next Action**: Orchestrator Report generation.

---

## 豕ｨ諢丈ｺ矩・

- 縺薙・繝輔ぃ繧､繝ｫ縺ｯ **蟶ｸ縺ｫ譛譁ｰ縺ｮ迥ｶ諷九ｒ蜿肴丐縺吶ｋ** 蠢・ｦ√′縺ゅｊ縺ｾ縺吶ょ推繝輔ぉ繝ｼ繧ｺ螳御ｺ・凾縺ｫ譖ｴ譁ｰ縺励※縺上□縺輔＞縲・
- Worker 縺ｯ菴懈･ｭ髢句ｧ区凾縺ｫ縺薙・繝輔ぃ繧､繝ｫ繧定ｪｭ縺ｿ縲∽ｽ懈･ｭ螳御ｺ・凾縺ｫ譖ｴ譁ｰ縺励※縺上□縺輔＞縲・
- Orchestrator 縺ｯ Phase 螟画峩譎ゅ↓縺薙・繝輔ぃ繧､繝ｫ繧定ｪｭ縺ｿ縲仝orker 縺ｫ繧ｿ繧ｹ繧ｯ繧貞牡繧雁ｽ薙※縺ｾ縺吶・
- 繝輔ぃ繧､繝ｫ繝代せ縺ｯ **邨ｶ蟇ｾ繝代せ縺ｧ險倩ｿｰ** 縺励※縺上□縺輔＞縲Ａls`, `find`, `Test-Path` 縺ｪ縺ｩ縺ｧ蟄伜惠遒ｺ隱阪＠縺ｦ縺九ｉ蜿ら・縺励※縺上□縺輔＞縲・

### 2026-02-11T00:00:00+09:00 - Orchestrator - P4 Ticketing Update
- Current Orchestrator Phase: P4 (Ticketing)
- Completed: TASK_031 (DONE), TASK_032 (DONE)
- Created: docs/tasks/TASK_033_DualGridHeightMap_ProfileMappingImplementation.md
- Created: docs/inbox/WORKER_PROMPT_TASK_033.md
- Created: docs/04_reports/REPORT_TASK_033_DualGridHeightMapProfileMappingImplementation_TEMPLATE.md
- Blockers (deferred): Unity compile/dependency verification chain (handled in separate tasks)
- Next Phase: P5 (Worker Delegation)
- Next Actions:
  1. Run Worker with docs/inbox/WORKER_PROMPT_TASK_033.md
  2. Generate docs/04_reports/REPORT_TASK_033_DualGridHeightMapProfileMappingImplementation.md
  3. Update mission status after Worker completion


### 2026-02-11T20:15:44+09:00 - Orchestrator - TASK_031 Reconciliation
- Integrated parallel stream updates for TASK_031.
- Verified REPORT_TASK_031_VerticalSliceKickoff_2026-02-11.md includes:
  1. M0-M1 checklist with owner/date/expected evidence
  2. mode comparison checklist (Noise/HeightMap/Combined/DualGrid)
  3. explicit handoff section to TASK_032
- Updated docs/tasks/TASK_031_TerrainVerticalSlice_Kickoff.md with reconciliation note.
- Consistency result: TASK_031 and TASK_032 remain DONE, next executable ticket is TASK_033.


### 2026-02-11T20:31:51+09:00 - Worker - TASK_033 Completion
- Task: docs/tasks/TASK_033_DualGridHeightMap_ProfileMappingImplementation.md
- Status: DONE
- Report: docs/04_reports/REPORT_TASK_033_DualGridHeightMapProfileMappingImplementation.md
- Key outputs:
  1. DualGridHeightSamplingEnums.cs
  2. DualGridHeightSamplingSettings.cs
  3. Profile integration in TerrainGenerationProfile.cs
  4. Profile-driven mapping/fallback in VerticalExtrusionGenerator.cs
- Deferred by constraint: Unity compile/runtime verification
- Next Orchestrator Action: move to report/integration phase and queue validation ticket if needed.

### 2026-02-11T23:39:34+09:00 - Orchestrator - P4 Ticketing (Validation Follow-up)
- Reason: TASK_033 completed with deferred Unity compile/runtime verification.
- Created: docs/tasks/TASK_034_UnityValidation_DualGridProfileMapping.md
- Created: docs/inbox/WORKER_PROMPT_TASK_034.md
- Created: docs/04_reports/REPORT_TASK_034_UnityValidation_DualGridProfileMapping_TEMPLATE.md
- Next Phase: P5 (Worker Delegation)
- Next Actions:
  1. Execute Worker prompt docs/inbox/WORKER_PROMPT_TASK_034.md
  2. Collect report docs/04_reports/REPORT_TASK_034_UnityValidation_DualGridProfileMapping.md
  3. Update mission flow to P6 after validation result

### 2026-02-12T01:56:00+09:00 - Worker - TASK_034 Static Verification Complete
- Task: docs/tasks/TASK_034_UnityValidation_DualGridProfileMapping.md
- Status: PARTIAL_DONE
- Report: docs/04_reports/REPORT_TASK_034_UnityValidation_DualGridProfileMapping.md
- Static verification results (all PASS):
  1. asmdef cross-references: Vastcore.Terrain -> Vastcore.Generation valid
  2. API backward compatibility: default parameter null preserves legacy callers
  3. Namespace/type reference consistency: all 3 new types correctly referenced
  4. Legacy fallback logic: preserved in WorldToSampleIndex and QuantizeHeight
  5. Serialization: [Serializable] class, non-null defaults, CopyFrom/ResetToDefaults safe
- Deferred to user:
  - Unity Editor compile check (zero new errors)
  - Runtime behavior verification (Play mode DualGrid scene)
- Next Orchestrator Action: await user Unity Editor verification, then P6 report generation

### 2026-02-12T02:02:57+09:00 - Orchestrator - Improvement Feasibility and Ticketing
- Input report accepted: TASK_034 remains PARTIAL_DONE (static PASS, Unity manual verification pending).
- Feasibility assessment:
  1. Auto compile validation automation: HIGH feasibility, immediate start.
  2. DualGrid inspector profile preview: MEDIUM feasibility, recommended after TASK_034 Unity verification.
- Created: docs/tasks/TASK_035_AutoCompileValidationAutomation.md
- Created: docs/inbox/WORKER_PROMPT_TASK_035.md
- Created: docs/04_reports/REPORT_TASK_035_AutoCompileValidationAutomation_TEMPLATE.md
- Created: docs/tasks/TASK_036_DualGridInspectorProfilePreview.md
- Next Phase: P5 Worker Delegation (TASK_034 user-gated, TASK_035 worker-ready)

### 2026-02-12T03:27:51+09:00 - Orchestrator - Handover Packet Expansion
- Added follow-up ticketing for immediate start continuity:
  1. TASK_036 worker prompt + report template
  2. TASK_037 closeout summary ticket + worker prompt
- Added quick-start handover doc: docs/04_reports/HANDOVER_2026-02-12_TerrainVerticalSlice_QuickStart.md
- Updated TASK_034 DoD to reflect partial completion reality.
- Push plan: commit all current changes except intentionally ignored Assets/Scripts/Tests/MCP.meta.

### 2026-02-14T00:00:00+09:00 - Orchestrator - SSOT再配線 & 状況棚卸し
- **SSOT再配線完了**: docs/SSOT_WORLD.md を最上位仕様として確立
  - 新規作成: SSOT_WORLD.md, ARCHITECTURE.md, ROADMAP.md, DOCS_INDEX.md, DEV_PLAN_ARCHIVE_2025-01.md
  - 既存文書に逆リンク追加: DEVELOPMENT_ROADMAP_2026.md, EVERY_SESSION.md, HANDOVER.md, README.md
  - Documentation/ 配下24ファイルを転送ページに置換
  - SSOT階層: SSOT_WORLD > ROADMAP_2026 > EVERY_SESSION > HANDOVER > タスクチケット
- **タスク棚卸し結果** (30チケット):
  - DONE: 18 / READY: 4 (PA-1, PA-3, PA-4, TASK_035) / OPEN: 2 (TASK_026, 027) / BLOCKED: 2 (TASK_021, 029) / PARTIAL_DONE: 1 (TASK_034)
- **2つの作業ストリーム並走中**:
  1. Phase A 安定化: PA-1, PA-3, PA-4 が READY（未着手）
  2. Terrain Vertical Slice: TASK_034 (PARTIAL_DONE, Unity検証待ち) → TASK_035/036/037 (READY)
- **ブロッカー**:
  - TASK_034 Unity Editor 検証がユーザーゲート
  - PA-1/PA-3/PA-4 は Worker 委譲で即時実行可能
- Next Phase: P3 (Strategy) — 2ストリームの優先度判断が必要

### 2026-02-17T15:05:36+09:00 - Orchestrator - P3 State Verification & Goal Framing
- Validated current SSOT alignment against docs/SSOT_WORLD.md and docs/01_planning/DEVELOPMENT_ROADMAP_2026.md.
- Current orchestration position remains P3 (Strategy): two-stream prioritization is pending.
- Active blockers confirmed: TASK_034 Unity Editor verification remains user-gated.
- Ready execution set confirmed: PA-1, PA-3, PA-4, TASK_035, TASK_036, TASK_037.
- Next recommended action: choose execution priority between Phase A stabilization stream and Vertical Slice validation stream.

### 2026-02-19T13:30:00+09:00 - Orchestrator - P4 Ticketing (SG/MG/LG initialization)
- Current Orchestrator Phase: P4 (Ticketing)
- Strategy summary:
  1. Short-term (SG-1): lock immediate execution queue for Phase A closeout
  2. Mid-term (MG-1): bridge Phase A completion to PB-1 quality foundation
  3. Long-term (LG-1): pre-ticket Phase C Deform completion stream
- Created: `docs/MILESTONE_PLAN.md`
- Created: `docs/tasks/TASK_PA-2_ProBuilderApiMigration.md`
- Created: `docs/tasks/TASK_PA-5_UnityCompileVerification.md`
- Created: `docs/tasks/TASK_PB-1_NUnitTestFoundation.md`
- Created: `docs/tasks/TASK_PC-1_DeformPackageIntegration.md`
- Blockers:
  - PA-5 remains BLOCKED until PA-2 is DONE.
  - TASK_034 Unity validation remains user-gated.
- Next Phase: P5 (Worker Delegation)
- Next Actions:
  1. Delegate PA-2 to Worker (highest priority)
  2. Execute TASK_035 in parallel if resources permit
  3. After PA-2 completion, run PA-5 compile verification and record `Unity Editor=コンパイル成功`

### 2026-02-20T15:58:44+09:00 - Orchestrator - P6 Verification Closeout (3-level validation)
- Compile guard execution completed: `scripts/check-compile.ps1` => pass (Exit 0).
- Ticket updates:
  1. `TASK_035_AutoCompileValidationAutomation` => DONE
  2. `TASK_PA-2_ProBuilderApiMigration` => DONE
  3. `TASK_PA-5_UnityCompileVerification` => DONE
- Added reports:
  - `docs/04_reports/REPORT_TASK_PA-2_ProBuilderApiMigration.md`
  - `docs/04_reports/COMPILE_VERIFICATION_2026-02.md`
- Added next Worker handoff:
  - `docs/inbox/WORKER_PROMPT_TASK_PB-1.md`
- 3-level validation scale applied:
  1. Level-1 (Gate): Unity compile pass and log evidence
  2. Level-2 (Task): DoD and report linkage updated per ticket
  3. Level-3 (Roadmap): SG/MG/LG queue rebalanced (`docs/MILESTONE_PLAN.md`)
- Next Phase: P3 (Strategy) -> P5 (Worker delegation for PB-1)
- Next Actions:
  1. Execute Worker with `docs/inbox/WORKER_PROMPT_TASK_PB-1.md`
  2. Decide order between `TASK_036` and `TASK_037` after PB-1 kickoff
  3. Keep `PC-1` as LG-1 planned item (not started)
