# Lumina UI Design System

## Product
- Desktop VPN client (Windows)
- Primary mode: Dark

## Style
- Minimal, premium, content-first
- Card-based surfaces, subtle elevation
- Consistent icon set (SVG path icons only)

## Color
- Use `Styles/Colors.axaml` tokens only
- Text contrast: avoid `TextMuted` for body text; prefer `TextSecondary`
- Focus color: `BorderFocus`

## Typography
- Font family: Inter / Segoe UI Variable / Segoe UI
- Prefer body text size ≥ 15 for long-form content
- Body line height: ~1.5

## Layout
- Default page padding: 40
- Keep reading width: max 720 for long text blocks
- Avoid horizontal scroll in page content

## Interaction
- Minimum hit target: 44x44 when possible (desktop exceptions allowed for title-bar controls)
- Always provide focus-visible indication for keyboard navigation
- Avoid layout shift on hover/press (use color/shadow/opacity; small scale only on isolated controls)

## Motion
- Micro-interaction duration: 150–250ms
- Prefer opacity/transform over size changes
