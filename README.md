# World War II – Aircraft (AR Scatterplot)

A Unity **Android AR** app for exploring **World War II aircraft data** as a **3D scatterplot in AR**. Place the plot on a real-world surface, filter aircraft by category, and tap points to view detailed aircraft info (stats + image/model).

## Features
- **AR placement**: place the scatterplot on a detected plane
- **3D scatterplot** visualization of aircraft stats
- **Filters**: Country, Role, State
- **Details on tap**: open a card with aircraft stats (and image/model if included)

## Tech Stack
- **Unity 6.0.62f1 LTS**
- **AR Foundation** (ARCore / ARKit via AR Foundation)
- C# scripts in `Assets/Scripts`

## Requirements
- Unity **6.0.62f1 LTS**
- Android device with **ARCore** support *(ARKit supported via AR Foundation if building for iOS)*
- Android Build Support installed via Unity Hub

## Getting Started
1. Clone the repo:
   ```bash
   git clone https://github.com/erfanslh/World-War-II-Aircraft.git
   cd World-War-II-Aircraft
