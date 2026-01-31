# World War II – Aircraft (AR Scatterplot)

A Unity Android AR app for exploring World War II aircraft data as a 3D scatterplot in AR.  
Place the plot on a real-world surface, filter aircraft by category, tap cubes to open rich detail cards,
and even drag 3D aircraft models out into your environment.

---

## Features

### AR & Visualisation
- AR placement – tap on a detected plane to place the scatterplot on the floor/table.
- 3D scatterplot – each aircraft is a cube positioned by 3 numeric attributes on X / Y / Z.
- Country flags on cubes – cube material shows the aircraft’s country flag.
- Dynamic axes – at runtime you can assign any of these attributes to X/Y/Z:
  - ActiveSince, MaxSpeed, Number, Wingspan, Length, Crew
- Axis ticks & labels – numeric ticks with readable formatting (integers for years/counts, 1–2 decimals for sizes).

### Interaction & Analytics
- Filtering
  - Filter by Country, Role (e.g. Fighter, Bomber, Transport), and State (Prototype, Retired, etc.).
  - Scatterplot rebuilds after each filter change (positions, axis ranges, ticks).

- DetailCard on tap
  - Tap a cube to open a DetailCard linked by a colored line.
  - Shows:
    - Basic info: name, role, manufacturer, years, dimensions, speed, crew…
    - Country flag image
    - 2D aircraft preview image (if available)
    - Embedded 3D aircraft model preview.

- Axis-focused analytics (per card)
  - For the three active axes (X/Y/Z) the card compares this aircraft to all cubes visible in the current plot.
  - Shows value + rank word (colored):
    - High → #006D00 (dark green)
    - Medium → #F2CC40 (dark yellow)
    - Low → #D94040 (red)
  - Special wording for ActiveSince:
    - early / average / late instead of low/high.

- Within Country / Within Role summaries
  - For each card, additional sections compare the aircraft:
    - to all aircraft from the same country, and
    - to all aircraft with the same role.
  - Uses the same color-coded ranking for the current X/Y/Z axes.

- Multi-selection comparison
  - You can have several DetailCards open at once.
  - Cards are arranged in an arc around the user’s head so they don’t overlap the plot.
  - Each card shows a “selection summary”:
    - compares the aircraft only to the other open cards,
    - highlights outliers (very high / very low values) on the selected axes.

### 3D Aircraft Models in AR
- Each DetailCard can spawn a 3D aircraft model:
  - Long-press the preview → model detaches from the card and drops onto the plane.
  - Drag with one finger → move the model on the surface.
  - Pinch with two fingers → scale the model.
  - Twist with two fingers → rotate around vertical axis.
  - Single tap on the placed model → delete it.
- When a world model is deleted, its small preview respawns on the DetailCard, so you can drag it out again.
- While a model is being dragged, global scatterplot gestures (rotate/scale plot) are temporarily disabled to avoid conflicts.

---

## Tech Stack

- Unity 6.0.62f1 LTS (URP / AR template)
- AR Foundation + ARCore XR Plugin (Android AR tracking & plane detection)
- C# scripts in Assets/Scripts
- TextMesh Pro for all 3D/World-space text
- Resources system for:
  - ww2aircraft.json dataset
  - Aircraft preview images (`Assets/Resources/AircraftImages`)
  - Country flag textures (`Assets/Resources/Country-Flags`)
  - Aircraft model prefabs (`Assets/Resources/AircraftModels`)

---

## Requirements

- Unity 6.0.62f1 LTS (or compatible Unity 6 version)
- Android device with ARCore support  
  (ARKit device would work in principle if built with ARKit via AR Foundation, but not tested)
- Unity Android Build Support & XR Plug-in Management installed

---

## Setup & Build

1. Open the project
   - Clone / unzip the repo.
   - In Unity Hub → Open → select the project folder.
   - Open scene: Assets/Scenes/SampleScene.unity.

2. XR / Android settings
   - File → Build Settings… → select Android → *Switch Platform*.
   - Edit → Project Settings → XR Plug-in Management  
     → Enable ARCore for Android.
   - Confirm Android SDK/NDK are installed via Unity Hub.

3.Build & Run
   - Connect an ARCore device via USB (developer mode enabled).
   - File → Build and Run.

---

## How to Use (On Device)

### 1. Place the scatterplot

1. Move your device around so AR can detect a flat surface.
2. Tap once on the plane → the WW2 aircraft scatterplot appears.
3. Gestures on empty space around the plot:
   - One-finger drag → rotate the entire plot.
   - Pinch → scale the plot.

### 2. Explore data points

- Each cube = one aircraft; its position comes from the currently selected X/Y/Z attributes.
- Cube material shows the country flag.
- Tap a cube:
  - Cube gets a breathing highlight.
  - A DetailCard appears in front of you, with a line connecting it to the cube.
- Tap the cube again or press the red X on the card to close it and remove the highlight.

### 3. Filters & axes

- Tap the “Filters” button under the plot:
  - Toggle countries, roles, and states.
  - Choose which attribute goes to X, Y, and Z.
- Close the panel → plot rebuilds with updated positions, axis ranges, and tick labels.

### 4. Reading a DetailCard

Each card includes:

- Basic details: name, role, manufacturer, years, crew, dimensions, max speed, etc.
- Country flag + optional 2D preview image.
- 3D model preview (if a prefab exists).
- Within Current Plot (X/Y/Z): colored high/medium/low (or early/average/late for year).
- Within Country: comparison to all aircraft of the same country.
- Within Role: comparison to all aircraft of the same role.
- Selection Summary (only if multiple cards are open):  
  compares this aircraft only to the other open cards, and marks outliers.

### 5. Working with 3D models

- Long-press the 3D model on a card to detach it.
- Move / scale / rotate it on the detected surface.
- Single tap to delete; the small model preview reappears inside the card.

---

## Dataset

- File: Assets/Resources/ww2aircraft.json
- Each AircraftRecord contains:
  - Name, Country, PrimaryRole
  - Production & service years: ActiveSince, LastBuilt, Retired
  - Number built, State, Crew
  - Geometry: Length, Wingspan, Height, WingArea
  - Performance: MaxSpeed
  - Optional: ModelPrefabName for linking to a 3D prefab.

---

## Known Limitations

- Tuned primarily for Android + ARCore; other platforms are not tested.
- Very small plot scales can make selecting tiny cubes harder.
- Analytics (ranking & outlier detection) are intentionally simple (percentiles / z-style thresholds) and meant for exploration, not precise statistics.

---
Erfan Mollasalehi, Zahra Borzoo - Immersive Analytics
