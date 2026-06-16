# CardFanUI

CardFanUI is a lightweight Unity package for displaying card hands in a **fan layout**.  
It is designed for card-based games (e.g. CCGs, deck-builders) and provides hover, adaptive spread, and drag & drop support.

# Detailed Documentation
https://pixitgames.com/docs/cardfan/
---

## ✨ Features
- Fan-shaped layout with adjustable **radius** and **arc angle**  
- **Adaptive spread**: single card centered, wider with more cards  
- **Hover effects**: lift, scale, straighten, bring-to-front overlay  
- **Drag & drop ready** with `UIDragCard` and `UIDropHandler`  
- Clear Inspector tooltips and customizable parameters  

---

## 🚀 Getting Started
1. Create a `Canvas` and add a `RectTransform` panel at the bottom of the screen.  
2. Add the **`UIHandLayout`** component to the panel.  
3. Create card prefabs with **`UIHandCardHover`** and add them as children of the panel.  
4. Adjust parameters in the Inspector:
   - `radius`  
   - `totalAngle`  
   - `adaptiveSpread`  
   - `baselinePadding`  
5. (Optional) Add **`UIDragCard`** to cards and **`UIDropHandler`** to drop zones for drag & drop support.  

---

## 📂 Folder Structure

Assets/CardFanUI/
Runtime/           # Core scripts
Samples~/Demo/     # Example scene and prefabs
Documentation/     # README.md and PDF docs
Editor/            # (Optional) custom editors

---

## 📝 Notes
- Tested with Unity **2021.3 LTS** and above.  
- Works with both **Screen Space - Overlay** and **Camera** canvases.  
- Assembly Definition: `CardFanUI.asmdef`.  
- This package is lightweight and can be integrated into existing projects easily.  

---

## 📜 License
Standard Unity Asset Store license applies.