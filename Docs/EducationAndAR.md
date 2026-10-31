# Educational and AR delivery — October 31, 2026

Target: November 2, 2026. Milestones are implemented incrementally on the existing
builder and simulator. All changes and commits are local; never push. `.agents`
and the unrelated untracked `dotnet-tools.json` remain excluded.

## Audit and execution order

Unity 6000.6.5f1, AR Foundation/ARCore/ARKit 6.6.2, URP 17.6.0, XRI 3.6.1,
Input System 1.20.0 and Test Framework 1.8.0 were rechecked in project/package
metadata. The existing desktop graph, directional rules, caches, database models,
resource queues, failure controls, packets and connection editing are preserved.
The AR/Components/Lessons/Quiz folders initially contained metadata only.
Android Build Support with SDK/NDK/OpenJDK is installed. iOS Build Support and
Xcode are absent on this Linux machine. No hardware AR validation is claimed.

Order: A shared AR, B Docker, C Kubernetes, D five lessons, E quiz/progress,
F platform builds/documentation, G coherent low-poly/mobile polish.

## A — Shared AR foundation

Open `Assets/SysARchitect/Scenes/ARWorkspace.unity`. It was created as a separate
Unity-authored scene derived from the existing desktop workspace, preserving the
original desktop and sample scenes and their GUIDs. AR uses exactly one existing
ArchitectureGraph and SimulationController, the existing inspectors and packet
visualization. Android and iOS providers are separately configured for ARCore and
ARKit; mobile loader initialization/running is enabled. XR Origin, camera offset,
AR camera/background, tracked pose input, session, horizontal plane/raycast and
anchor managers are wired explicitly. No packages were upgraded.

Scan a horizontal textured surface, then tap a highlighted detected plane. A
native AR anchor owns the 0.84 m square workspace (2.4 model units at 0.35 scale).
Choose a component in the scrollable mobile toolbox, then tap within the workspace.
Tap nodes/lines to select; drag nodes to move; Connect takes two taps. Existing
node/connection delete controls, role/configuration panels, run/pause/step,
resources, database modes and failure/restart remain shared. A safe-area container,
toolbox toggle and Reposition workspace control are included. Reposition retains
the architecture. Lost session/anchor tracking pauses editing and simulation;
unsupported devices and failed anchoring get explanations.

`WorkspaceTouchInput` adapts touch phases into existing placement, movement,
selection and connection commands. Its input ownership flags prevent emulated
mouse events from also editing the graph. UI-originated and multi-touch gestures
are rejected. Workspace bounds and tracking gates are explicit. AR owns no copy
of routing or simulation logic. Graph nodes, edge widths/ports and packets inherit
workspace scale, while desktop scale remains one.

Files: `Scripts/AR/{ARWorkspace,WorkspaceTouchInput,MobileWorkspaceUI}.cs`, shared
interaction controllers, graph parenting and scale-aware visual adapters;
`Editor/ARWorkspaceSetup.cs`, the new AR scene/plane/mobile button assets,
runtime assembly references, XR provider flags, and `SharedTouchTests.cs`.

Validation: Unity scene generation/compilation passed; **19/19 Play Mode tests
passed**, including all 17 prior desktop cases and two shared-touch/scale cases.
Results `/tmp/sysarchitect-ar-play.xml`, log `/tmp/sysarchitect-ar-play.log`;
scene setup log `/tmp/sysarchitect-ar-setup.log`. Synthetic touch phases validate
the common adapter and actual Unity raycasts, not camera permission, physical
plane detection, native anchoring or device tracking. Those remain hardware checks.

The stable Linux launcher now uses `Builds/Linux-Current` so historic demo folders
remain intact. Each desktop build includes its visible version and BUILD-INFO.txt.
The A checkpoint built successfully as `0.4.0-ar-foundation` (108,710,737 bytes),
log `/tmp/sysarchitect-ar-linux-build.log`; an eight-second graphical Linux
startup check passed with no application exception (intentional timeout exit 124).
Player log `/tmp/sysarchitect-ar-linux-player.log`. The original desktop scene
hash remains unchanged. Native horizontal-plane/anchor/camera behavior and touch
ergonomics still require compatible Android/iOS hardware. Next priority: B Docker.

## B — Docker simulations

The scrollable toolbox adds one Docker Host. Select it to open Docker Explorer,
choose a reusable `web:v1`, `api:v1` or `worker:v1` image, and create up to eight
container instances. These are actual existing Container graph nodes, with their
own queues, processing/memory limits, resource counters and failure state. The
read-only image descriptor is shared; instance state is separate. Images represent
packaged applications; containers represent isolated processes sharing a host
kernel, not separate virtual machines. No Docker daemon or shell commands run.

Frontend > Docker Host > owned Containers routes real requests. A host skips
unavailable instances and cannot dispatch to another host's containers. Its
capacity is a shared throughput/dispatch budget, so extra instances cannot exceed
that budget. Host memory includes contained instance memory. Each container still
has an independent memory ceiling; host exhaustion can affect the whole group.
The existing engine owns all request/resource processing; the new model only
owns image/instance membership and issues stop/restart commands. Host outages
make contained services unavailable and fail their in-flight work; their own
state is retained. This is an educational resource model, not Linux cgroups.

Docker Explorer provides create, start, stop, restart, crash, remove, expand/
collapse, instance choice, CPU quota and memory-limit controls. Start/restart uses
the engine's one-second recovery in simulation time. Run/Step is required.
Expand shows actual contained nodes inside a holographic outline. They are
selectable; their layout moves with the host. Removing a host also removes its
owned graph nodes and edges, preventing orphaned simulator entries.

Files: `Components/DockerHostModel.cs`, `DockerWorkspaceController.cs`,
`UI/DockerUI.cs`, `Visualization/WorkloadEnclosure.cs`, `Editor/DockerSceneSetup.cs`,
the shared graph/engine ownership rules, toolbox/type/palette additions, and both
Unity-authored workspace extensions. Existing GameObject IDs/metas were preserved.

Validation: **55 Edit Mode and 20 Play Mode cases passed**, including prior
desktop/AR touch cases, reusable images with independent instances, stop/start/
restart, shared host capacity, host RAM/exhaustion/conservation, foreign-container
routing rejection, actual UI creation, picking inside the enclosure and recursive
ownership cleanup. `/tmp/sysarchitect-docker-core.xml` and
`/tmp/sysarchitect-docker-play.xml` contain results; same-basename `.log` files
record execution. The inspected enclosure capture is `/tmp/sysarchitect-docker.png`.
CPU display retains the existing capped 50 ms utilization average; low service
budgets have visible quantization. Throughput uses exact service credits.
The Docker Linux build passed as `0.5.0-docker` (108,745,229 bytes), log
`/tmp/sysarchitect-docker-linux-build.log`. The current launcher uses that build;
the previous A artifact is preserved in `Builds/Linux-ARFoundation`. Device AR
and end-to-end physical touch remain unverified. Next priority: C Kubernetes.

## C — Kubernetes deployments and recovery

The toolbox adds a Kubernetes Cluster grouping a Service gateway and Deployment
control. Select it, add up to four real Worker graph nodes, choose an explicit
desired replica target (0–8), and Run/Step. A deterministic 0.5-second controller
reconciliation creates actual Pod graph nodes on healthy workers, using four
educational slots per worker. New Pods take one simulated second to be ready.
The panel distinguishes desired, actual and ready counts. Lack of worker capacity
leaves a visible pending explanation; it does not fabricate processing capacity.

Fail Pod represents terminal Pod failure. The controller removes that identity
and creates a new Pod UID to restore the same target. Changing desired replicas
is explicit horizontal scaling; it has separate counters. **HPA is not configured**
and high traffic alone never changes the target. Container restart policies,
production scheduler constraints and real kubelet/API interactions are outside
this bounded educational model. Deleting a managed Pod also triggers replacement
on the next reconciliation; deleting the cluster removes its owned hierarchy
without recreating it. Reset clears controller observation counters, retaining
the explicit configuration and existing graph identities.

The expanded hierarchy reuses WorkloadEnclosure and actual selectable graph
members. Frontend > Cluster Service > healthy Worker > ready Pod follows the
existing engine's queues, capacities, resource metrics and packet events. Arrows
abstract service/worker dispatch: application requests do not traverse the API
server, Deployment controller or scheduler. Worker CPU budgets bound their Pods,
and worker memory includes Pod usage. Managed routes cannot target foreign nodes.

Files: `Components/DeploymentModel.cs`, `KubernetesWorkspaceController.cs`,
`UI/KubernetesUI.cs`, `Editor/KubernetesSceneSetup.cs`, core type/ownership rules,
tick/reset events, nested enclosures, both scene extensions and regression tests.

Technical references checked against primary documentation:
[Kubernetes workload controllers](https://kubernetes.io/docs/concepts/workloads/controllers/),
[replica replacement](https://kubernetes.io/docs/concepts/architecture/self-healing/),
[explicit horizontal scaling](https://kubernetes.io/docs/tasks/run-application/scale-deployment/),
[HPA configuration](https://kubernetes.io/docs/reference/kubernetes-api/autoscaling/horizontal-pod-autoscaler-v2/),
[Docker images](https://docs.docker.com/get-started/docker-overview/), and
[container isolation/shared kernel](https://docs.docker.com/get-started/docker-concepts/the-basics/what-is-a-container/).

Validation: **59 Edit Mode and 21 Play Mode cases passed**. These cover new
identity replacement without changing desired count, explicit scale-up/down,
absence of implicit HPA, pending worker slots, readiness delay/reset, UI controls,
real multi-hop traffic, and hierarchy deletion without resurrection. Results:
`/tmp/sysarchitect-kubernetes-core.xml` and `/tmp/sysarchitect-kubernetes-play.xml`;
the same-basename logs record execution. The inspected rendering is
`/tmp/sysarchitect-kubernetes.png`. A Linux build succeeded as
`0.6.0-kubernetes` (108,771,597 bytes), log
`/tmp/sysarchitect-kubernetes-linux-build.log`. The prior Docker artifact is in
`Builds/Linux-Docker`; the stable launcher remains `Builds/Linux-Current`.
Remaining scope limits: bounded scheduler/readiness abstraction, no actual
Kubernetes cluster or HPA. Next priority: D guided lessons.

## D — Five guided learning modules

Lessons / Progress in the toolbox opens a scrollable guide. Next lesson cycles
through client-server, load balancing/scaling, database replication/sharding,
Docker images/instances and Kubernetes orchestration/recovery. Each definition
contains measurable objectives, a short undergraduate-level explanation, four
practical steps, a conceptual algorithm and a challenge. Previous/Next step
changes the guidance; it never drives fake traffic or controller outcomes.

Load example is explicitly labelled as clearing the workspace. It pauses and
resets the existing simulation and builds an editable scenario with actual graph
nodes/controllers. Existing user architectures can also be used without loading
an example. AR examples are gated until a workspace is placed/tracked. Close the
guide to use the normal builder and resource panels, then reopen it for the next
step. The highlighted algorithm follows actual typed transfer/outcome events,
database pending copies, container state/configuration and deployment actions.
Explicit writes use their real transfer kind, not a request-ID guess.

Check challenge evaluates measured state before saving local completion:

- Client-server: actual directed three-tier path, 20 new successes and DB reads/writes.
- Load balancing: two backend targets with storage, at least 20 operations at each,
  100 new successes, observed routing to a healthy target while another was crashed,
  then both operational again.
- Database: primary writes, replica reads and pending-copy lag, then at least three
  shards with records on multiple partitions of that same Database.
- Docker: two operational instances of one image, changed limit, observed stop/start,
  crash and same-instance recovery with subsequent work, and 20 new successes.
- Kubernetes: explicit target change to three, actual new-identity failure replacement,
  three ready replicas and 20 new successes.

Incomplete challenges display the missing action. Prior request counters are
excluded from newly selected lessons. Reset clears practice observations, while
already completed lessons remain saved locally. The PlayerPrefs JSON store uses
a versioned application-specific key; tests inject an in-memory store and do not
overwrite the developer's progress. No user study or learning gain is asserted.

Files: `Lessons/LessonCatalog.cs`, `LessonController.cs`, `LearningProgress.cs`,
`UI/LessonUI.cs`, `Editor/LessonSceneSetup.cs`, both workspace extensions and
lesson/persistence regression tests.

Validation: **62 Edit Mode and 26 Play Mode cases passed**. Five end-to-end
challenge tests verify the actual simulation actions and reject premature
completion; persistence tests cover round-trip, duplicates, corrupted/unknown
data and complete module content. Results `/tmp/sysarchitect-lessons-core.xml`
and `/tmp/sysarchitect-lessons-play.xml`; rendering
`/tmp/sysarchitect-learning-guide.png` was inspected. UI canvas order was corrected
so world labels stay behind the readable guide. One core invocation was externally
terminated before tests and produced no results; a fresh run completed successfully
(interrupted log `/tmp/sysarchitect-lessons-core-interrupted.log`).
Linux build `0.7.0-guided-lessons` succeeded (108,817,837 bytes), log
`/tmp/sysarchitect-lessons-linux-build.log`. The prior C checkpoint is preserved in
`Builds/Linux-Kubernetes`. Local completion is implemented; quizzes and score
persistence follow in E. No learning improvement or user-testing results are claimed.

## E — Pre/post quizzes and local progress

The guide adds Pre-quiz, Post-quiz and Scores / Progress summary. The bank contains
20 original multiple-choice questions, four per lesson, including scenarios about
disconnected graphs, bottlenecks, failover, database roles/lag/sharding/cache hits,
container isolation and deployment recovery versus scaling. Pre and post each use
10 questions (two per module), with disjoint fixed forms. Use pre before the
lessons and post afterwards. The forms are practice instruments; their scores do
not constitute a controlled measurement of learning effectiveness.

Every answer reveals correct reasoning. Answered items cannot be resubmitted and
unanswered items cannot advance. Only completed attempts are saved, immediately
after the final answer; closing an incomplete quiz creates no score. Quiz input
pauses simulation and suspends desktop/touch graph editing, then restores the
prior input ownership on close. The architecture is retained and simulation
stays paused until Run/Step. Escape/back also closes the quiz.

Local progress extends the existing versioned JSON record, preserving D's lesson
completion. Each saved attempt includes form stage, question IDs, actual choices,
recomputed correct/total and UTC completion time. Stored answer IDs/indices are
validated; scores are derived from the answer key instead of trusting stored
totals. The latest 20 completed attempts are retained. Summary shows five module
completion states and the latest real pre/post scores; missing quizzes show
"not taken", not a fabricated zero or baseline. There is no cloud sync or user
study data. Test profiles use in-memory storage.

Files: `Quiz/QuizCatalog.cs`, `QuizSession.cs`, extended
`Lessons/LearningProgress.cs`, `UI/QuizUI.cs`, `Editor/QuizSceneSetup.cs`, both
workspace extensions and quiz/persistence/input-restore regression tests.

Validation: **66 Edit Mode and 28 Play Mode cases passed**. Coverage includes all
bank/form constraints, answer gating, double submission, scoring, completed-only
and once-only persistence, score recomputation, old-record compatibility, bounded
history, both full UI forms, summary values and input restoration after an
unfinished attempt. Results `/tmp/sysarchitect-quiz-core.xml` and
`/tmp/sysarchitect-quiz-play.xml`, with same-basename logs. Inspected captures:
`/tmp/sysarchitect-quiz-feedback.png` and `/tmp/sysarchitect-progress-summary.png`.
Those screenshots use scripted in-memory test answers (9/10 pre, 10/10 post),
not real learner results or evidence of improvement; the application is not seeded
with those scores.
Linux build `0.8.0-quizzes-progress` passed (108,864,989 bytes), log
`/tmp/sysarchitect-quiz-linux-build.log`. The prior D build is preserved in
`Builds/Linux-Lessons`. Remaining checks include physical mobile input and device
AR; no external deployment success is claimed. Next priority: F platform builds.
