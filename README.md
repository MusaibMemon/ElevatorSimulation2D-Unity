# Unity 2D Elevator Simulation

This project is a 2D elevator simulation developed in Unity as part of a technical assignment.

## Features

- 3 independent elevators
- 4 floors (Ground, 1, 2, 3)
- Floor call buttons
- Nearest elevator selection logic
- Direction-aware request handling
- Separate request queue for each elevator
- Smooth elevator movement between floors
- Real-time elevator status display
- Built-in multi-level gameplay with progressive traffic scenarios
- Automatic level progression, retry-on-fail, and optional level status HUD

## System Design

The system uses a central **ElevatorManager** that assigns requests to the best elevator based on:

- Distance to requested floor
- Current elevator direction
- Pending requests

Each elevator maintains its own request queue and processes them sequentially.

## Technologies Used

- Unity Engine
- C#
- Unity UI (Canvas & Buttons)

## Project Structure

Scripts include:

- ElevatorManager.cs
- ElevatorController.cs
- FloorCallButton.cs

## Level Gameplay

The simulation now includes an optional level mode (enabled by default) that introduces escalating call-wave scenarios:

- **Morning Warm-Up** (light traffic)
- **Office Rush Hour** (faster mixed demand)
- **Evening Peak** (sustained heavy traffic)

Each level has a time limit and scheduled hall calls. A level is completed once all issued calls are served, then the next one starts automatically. If time runs out, that level restarts.

## Author

Musaib Memon
