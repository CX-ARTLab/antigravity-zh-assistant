# Design QA

## Comparison target

- Source visual truth: `C:\Users\du17\AppData\Local\Temp\codex-clipboard-b30b45bc-0204-4446-83df-e74560565e0d.png`, with the red line treated as the requested bottom crop rather than interface content.
- Normalized source crop: `C:\Users\du17\Documents\Codex\2026-08-28\x20\work\annotated-target-crop-v3.png`
- Implementation screenshot: `C:\Users\du17\Documents\Codex\2026-08-28\x20\work\assistant-preview-v3-150.png`
- Combined comparison: `C:\Users\du17\Documents\Codex\2026-08-28\x20\work\design-qa-comparison-v3.png`
- Viewport/state: Antigravity onboarding/login screen, light theme, localization active.
- Normalized source and implementation pixels: 750 x 825. Both were compared at the same native 150% Windows display density, corresponding to a 500 x 550 logical window.

## Final comparison

- Fonts and typography: the title uses Segoe UI Variable Display Semibold at the source's 24 px logical scale. The assistant row is bold, the primary action is 15 px bold, the information row is reduced to 12 px, and option text remains 12 px. Weight, centering, wrapping, and hierarchy match the source while accommodating the requested Chinese copy.
- Spacing and layout: the logo, title, 344 x 182 logical card, 272 x 40 logical primary action, information row, and option row align with the source positions. The card's three content rows use four approximately 24 px logical vertical gaps. The window ends at the annotated red line; all content below it is removed. Radii and elevation are DPI-scaled.
- Colors and tokens: body `#EAECF0`, primary `#8839EF`, title/foreground `#4C4F69`, card/secondary surfaces, muted option text, and title-bar surface match the extracted Antigravity tokens.
- Image quality and assets: the glow and Antigravity logo are rasterized directly from the installed app's original SVG assets; no placeholder or reconstructed logo is used.
- Copy and content: the requested replacements are present: `汉化助手 v0.6.7`, `汉化已生效`, dynamic installed Antigravity version plus pending-adaptation count as plain text, `自动更新`, and `开机启动`. `上一步` and `下一步` are absent.
- Focused region evidence: the full-resolution combined image keeps the title, card, buttons, and option row readable, so a separate crop was unnecessary.

## Comparison history

1. Initial capture found a P1 DPI-layout failure: the 500 x 874 logical layout rendered into a 333 x 583 surface and clipped the right side. Fixed by restoring WinForms DPI scaling, deriving the runtime scale, scaling the frame, and scaling custom background drawing. Post-fix capture measured 750 x 1311 with no clipping.
2. Second comparison found P2 typography and corner-radius drift: pixel-unit fonts and custom radii were not scaling at 150% display density. Fixed by scaling visible fonts, checkbox geometry, icons, card/button radii, and muted option colors. The final combined comparison shows matched hierarchy, placement, palette, and source assets.
3. User capture found a P1 150% DPI regression: the 750 x 1308 target was being scaled again to 1125 x 1965. Fixed by disabling WinForms automatic rescaling, applying one explicit DPI scale to the frame and visible controls, and setting the logical viewport to 500 x 872. The post-fix capture is exactly 750 x 1308 at 150% with no clipping or double scaling.
4. The requested refinement removed the footer navigation and secondary button surface, changed version/adaptation status to smaller plain text, strengthened assistant and primary-action typography, and switched the title to Segoe UI Variable Display Semibold. The revised same-size comparison shows no remaining P0/P1/P2 mismatch.
5. The annotated crop request shortened the logical frame from 500 x 872 to 500 x 550 (750 x 825 at 150%) and exposed uneven card gaps. The frame now terminates at the marked line, and the card rows were redistributed with approximately 24 px logical gaps above, between, and below the content. The final normalized side-by-side comparison shows the intended crop and balanced card rhythm with no clipping.

## Findings

- No actionable P0, P1, or P2 differences remain.
- P3: Chinese replacement strings naturally have different text widths from the original English labels; they remain centered and do not alter component geometry.

## Interaction checks

- The primary localization control reached the active `汉化已生效` state against the running Antigravity instance.
- Automatic update and startup controls remain wired to the existing settings handlers.
- Window minimize, close, drag, single-instance activation, tray behavior, and background monitoring code paths are preserved.

final result: passed
