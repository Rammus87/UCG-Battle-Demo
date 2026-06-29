# 02 Playmat Surface Spec

Scope: first formal art-spec document only. Do not modify Unity C#, Prefab, Scene, settings, or image files while following this document.

Target output asset:

- `.agents/components/artpack/unity-ready/02_playmat_surface.png`

## Asset Purpose

- Serves as the tabletop playmat surface for `UCG-Battle-Demo`.
- Provides the grounded table/play surface layer beneath the battlefield composition.
- Does not include Lane, card slot, Deck, Trash, card, hand, text, or UI elements.
- Unity will later layer Lane guides, Slot frames, cards, scene container, Deck/Trash UI, and feedback overlays above this asset.
- This asset should read as the physical/mid-background table surface, not as an interactive gameplay marker.

## Visual Direction

- Real tabletop card-game feeling.
- Dark premium cloth playmat surface.
- Warm tabletop atmosphere, as if placed on a softly lit desk or game table.
- Subtle technology mood: understated grid fibers, faint embedded glow, restrained holographic sheen, or barely visible circuit-like weave.
- Low contrast enough that cards, lanes, slots, and UI remain readable above it.
- Wide horizontal composition that supports camera movement without obvious repeated landmarks.
- Do not create a floating arena.
- Do not create an outer-space, galaxy, nebula, or cosmic background.

## Asset Restrictions

The PNG must not contain:

- Cards.
- Card backs.
- Text, logos, numbers, labels, symbols, or readable marks.
- Fixed gameplay zones.
- Lane lines.
- Card slots.
- Deck or Trash areas.
- UI panels, buttons, prompts, meters, or toast frames.
- Hand cards or fan-shaped card layouts.
- Character silhouettes or battlefield units.

## Suggested Size

- Recommended canvas: `4096 x 1920`.
- Aspect ratio: wide horizontal surface, approximately `2.13:1`.
- Should be horizontally extendable or crop-safe.
- Must support future `3` to `8` lane layouts where the camera can move left and right.
- Avoid strong center-only composition; visual detail should remain useful across the full width.
- Keep important texture detail away from extreme edges when possible, so Unity can crop or overscan safely.

## Camera / Viewport Behavior

- `02_playmat_surface.png` is a horizontal world surface, not a fixed full-screen background.
- The initial battle view should start on the right side of the playmat surface.
- As more battle lanes are revealed or the battle route expands, the Camera / viewport should progressively move left across the same horizontal surface.
- Do not use a fixed center crop as the default presentation.
- Do not scale the playmat into the phone `9:16` viewport.
- Unity should keep a controllable horizontal viewport offset for this layer, so the visible playmat window can shift independently from the source image.
- The playmat should preserve its source aspect ratio while the viewport chooses which horizontal region is visible.
- The visible region may crop horizontally as the viewport moves, but it should not distort the playmat texture.
- Future scene-card effects may replace the playmat skin; the horizontal viewport offset behavior should remain reusable across skins.

## Unity Integration Position

Based on `UNITY_INTEGRATION_PLAN.md`, the future Unity mount point should be:

- Primary target: `Canvas/Battlefield Visual Layer`.
- Layer intent: above `Canvas/UCG HUD Background` / base battlefield background, below lane guide lines, slot frames, scene container, cards, Deck/Trash UI, toasts, and card feedback overlays.
- Practical placement: a decorative child Image under `Battlefield Visual Layer`, near or below generated children such as `Battlefield Scene Pedestal Wash`, lane guide visuals, and character grounds.
- The Image must remain decorative only: `raycastTarget = false`.
- It must not become the parent of gameplay zones or pointer targets.
- It must not alter `Viewport`, `Content`, `Lane N`, `Player Slot`, `Opponent Slot`, `SharedSceneSlot`, card roots, or any hitbox-related RectTransform.

Suggested future object name:

- `02_playmat_surface.png`

Suggested future role:

- A broad non-interactive surface texture that visually grounds the battle area while allowing Unity-generated Lane, Slot, scene, card, and UI layers to remain authoritative.

## Production Notes

- Export with transparency only if the playmat is meant to blend over `01_battlefield_bg_base.png`; otherwise an opaque PNG is acceptable if it fully covers the intended playmat area.
- Keep the surface darker than active cards and UI.
- Avoid high-frequency noise under card text regions.
- Avoid bright fixed lines that could be mistaken for Lane guides.
- Avoid distinct rectangular marks that could be mistaken for card slots.
- If AI-generated, use negative prompts matching the Asset Restrictions section.
- Final import should use Unity `Sprite (2D and UI)`.
