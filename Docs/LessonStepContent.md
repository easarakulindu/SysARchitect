# Guided instruction content correction — November 1, 2026

## Recovered state and root cause

Recovered from `d7ce945` with seven partially edited implementation files and an
untracked viewport regression. The interrupted focused Unity run had no result
XML; it was not counted as a pass. No new scene or lesson system was generated.

The 0.11.1 implementation already had four distinct instruction strings for each
of five lessons, and its refresh key included the instruction index. Next/Previous
updated the body string correctly. However, the displayed title was lesson-level;
static navigation, objectives and the long lesson overview preceded the changing
instruction, and mobile action rows preceded the explanation panel. Scrolling was
not reset on step navigation. The first viewport could therefore remain identical
while the step counter changed. Dedicated step headings/explanations were missing.

The preserved baseline regression (`sysarchitect-step-content-reproduction.xml`)
failed because actual TMP lines within the viewport were identical before and
after Next step, even though the full body string and step index changed. This
reproduces the reported presentation failure in the actual mobile scene in Unity;
it is separate from the user's physical Redmi Note 13 4G / APK 0.11.1 observation.

## Corrected presentation

- All twenty existing instructions now have matching headings, explanations and
  component focus metadata, aligned with their original lesson goals. Existing
  instruction strings and explicit demonstration actions are retained.
- The pinned title includes the current step heading. The scroll body starts with
  that heading, the instruction and **Why this step**, followed by component focus,
  navigation help, objectives, overview, the existing live algorithm and challenge.
  Mobile action/quiz rows remain reachable below the explanation by scrolling.
- Forward/backward instruction navigation and topic selection reopen content at
  the top. Live algorithm events do not keep resetting the learner's scroll.
  The step counter remains bounded; Previous/Next lesson and Lessons are separate.
- Mint focus applies only to existing matching node types and links whose two
  endpoints match the current step. Closing the guide or opening the picker clears
  it. This does not select nodes, create links, load examples, advance time, inject
  failures or change database/container/cluster configuration. Selection and actual
  traffic colors retain priority; failure and overload indicators are preserved.
- Completion and quiz storage are unchanged. Revisiting a topic starts instruction
  one without erasing progress. The existing live conceptual algorithm remains
  tied to actual engine/controller observations, independent of instruction index.

## Verification

Exact editor: Unity **6000.6.5f1**, unchanged packages, isolated project
`/tmp/SysARchitectValidation`. Unity jobs run serially with two job workers and one
import worker to reduce load after the host crash. No root editor was commandeered.

- Edit Mode: **96 passed, 0 failed** (`sysarchitect-step-content-editmode.xml`).
  Includes five new data cases for matching, distinct content and valid focus types.
- Play Mode: **47 passed, 0 failed** (`sysarchitect-step-content-playmode.xml`),
  including all existing desktop, graph/routing, resource, education and AR-adapter
  regressions. The final test run also checks visible explanation text, not only
  headings and instruction indices. Android delivery follows this verified source.
- The new mobile fixture exercises all five topics, all four instructions forward
  and backward, both boundaries, switching/revisiting and explicit bottom-to-top
  scroll restoration at 1280×720, 1920×1080, 2400×1080 and 2796×1290. The latter
  sizes use synthetic safe-area insets. Assertions inspect actual rendered TMP
  viewport lines, full instruction/explanation, title, counter and button bounds.
- The desktop fixture checks actual content, focus/selection/traffic priority and
  unchanged graph identities, connection selection, simulation time and successes.
  Two completed lessons and completed pre/post quiz fixtures survive navigation
  and JSON reload after a scene reload. It uses memory storage rather than changing
  the developer's PlayerPrefs; this is not an Android storage/device test.
- Render captures were inspected at 1280×720 and 2400×1080. The current instruction
  and explanation are visible immediately, with separate pinned topic/step controls.

The prior APK and both saved scenes were hash-checked against the pre-interruption
snapshot. Package files, unrelated material edits and `dotnet-tools.json` are
unchanged. `.agents` remains ignored and excluded. Linux binaries are preserved;
desktop source behavior is tested in Unity rather than claiming a new Linux build.

No physical Android retest, AR tracking verification or device performance profile
is claimed. After installation, verify all five topics and steps on the Redmi,
close/reopen the application and confirm prior completion/quiz results remain.
