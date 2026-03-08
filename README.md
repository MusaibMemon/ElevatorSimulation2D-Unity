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

## Author

Musaib Memon
