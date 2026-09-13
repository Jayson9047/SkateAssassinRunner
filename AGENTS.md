# ELROI project instructions

This is an ELROI project. Its persistent engineering knowledge lives at
`C:\Obsidian_Vaults\ELROI_Brain` (the canonical vault on this machine).
Do not substitute another vault. If inaccessible, report the problem.
The current workspace may be this vault or a separate Unity project; inspect
its actual structure before making assumptions. Work only in authorized projects.

Use the **elroi-knowledge** skill for ELROI tool workflows, implementations,
onboarding, knowledge retrieval, and documentation decisions. Read its exact
entrypoint even if it is absent from the skill selector:
`C:\Obsidian_Vaults\ELROI_Brain\elroi-knowledge\SKILL.md`.

- Run the Ambiguity Gate only when missing information materially changes the work.
- Search the vault's Tool Stack Index first; inspect candidate Workflow Indexes.
  A tool match is not a workflow match. Then search previous implementations,
  compose known capabilities, and research only the remaining gaps.
- Route by explicit primary system, intent, capability, write scope, and existing
  workflow ownership. Aliases and related assets are discovery signals only.
- Follow the skill's ownership rules for vendor docs, bundled docs, internal
  source/tests, and hybrid dependencies. Vendor claims are source knowledge,
  never verified ELROI experience.
- For relevant imported assets missing vendor docs in the vault, copy their
  bundled documentation into `Vendor_Docs/<Asset Name>/`, preserve provenance,
  and register/merge SOURCE.md plus the Vendor Source Index. Follow the skill's
  shortcut-only documentation rule. A Tool Stack entry alone is not sufficient.
- Follow the reader/writer contracts in the vault's `_Schemas` directory.
  Preserve stable IDs; merge existing owners; maintain indexes. Preserve actual
  folder names, including this vault's `Vendor_Docs`.
- Track cumulative accepted and in-review portions in `_Working`. Canonical
  knowledge contains one final accepted state, never rejected attempt history.
- Silently run documentation eligibility after meaningful work. Document new
  reusable discoveries, not trivial changes or unchanged reuse. Present the
  three-option acceptance protocol only for eligible candidates; batch approval
  by tool/stack. Canonical promotion requires implementation acceptance and
  documentation approval; respect approval already given in the session.
- Keep exact verification versions per workflow. Never update every workflow's
  version because a package changed. Record material glue in implementations
  with project, versions, purpose, and exact `./Assets/...` paths.

Keep tools, implementations, source documentation, and working state separate.
Do not create notes merely because a package was imported. `Elroi_Projects` is
reserved for future project requirements automation; do not implement that system
unless explicitly requested. Copy this file into future ELROI project roots;
the knowledge and skill remain in the canonical vault.
