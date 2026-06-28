# MISSION.md

# UCG-Battle-Demo Mission

> **Create a production-quality battle tutorial, not a Unity demo.**

---

# What We Are Building

UCG-Battle-Demo is the official battle tutorial module for **UCG-tool**.

It is not a prototype.

It is not a Unity sample.

It is not a debug project.

Every change should move the project closer to a polished commercial card game experience.

---

# Current Mission

The core gameplay is already implemented.

**Do NOT expand gameplay.**

Current development focus:

* Art Polish
* Game Feel
* Visual Quality
* UI Consistency
* Battlefield Presence
* Card Presentation

---

# Current Target

The only visual target is:

**Target UI v1**

Treat Target UI v1 as the project's Art Bible.

Do not invent a new style.

Do not imitate other card games.

Study its design language and rebuild it for UCG.

---

# Visual Priority

Every screen should naturally guide the player's eyes.

Priority:

1. Battlefield
2. Cards
3. HUD
4. Background

The background should support the battle, never dominate it.

Cards are always the hero.

---

# Design Philosophy

Never solve problems by adding more Panels.

Instead improve quality through:

* Hierarchy
* Glass
* Shadow
* Lighting
* Corner Details
* Thin Lines
* Battlefield Details
* Better Card Presentation

Less UI.

More Game Feel.

---

# Never Do

Never:

* Turn the project back into a Unity demo
* Add large transparent panels
* Add Battle Boards to hide empty space
* Add unnecessary Rectangles
* Add excessive Glow
* Add Prototype-style UI
* Change gameplay unless requested

---

# Always Do

Always ask:

**Does this make the game feel more like a finished commercial product?**

If the answer is no,

stop and redesign.

---

# Before Every Task

Before writing any code:

1. Read UIAGENTS.md
2. Read all files inside `.agents/docs`
3. Compare Current Screenshot with Target UI v1
4. Decide the smallest improvement that moves the project closer to Target UI v1

---

# Definition of Success

Success is **not**:

> "The feature works."

Success is:

> "Players forget they are looking at a Unity project."

When players see the screen for the first time, the expected reaction is:

> "This looks like a real card game."

Not:

> "This looks like a Unity demo."

---

# Final Principle

Every commit should improve one of these:

* Better Battlefield
* Better Cards
* Better Readability
* Better Game Feel
* Better Polish

If a change does not improve one of them,

it probably should not be made.
