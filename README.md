# Procedural Rigging & Control Generation Tool (Unity)

A Unity-based procedural animation tool built to explore technical animation systems, rig automation, and inverse kinematics workflows. This project focuses on replicating production-style rigging concepts inside Unity, similar to tools used in DCC software like Maya.

---

## Overview

This tool automates character rig setup by generating control rigs, solving inverse kinematics, and adding procedural motion behaviors. It is designed for learning and experimentation in technical animation programming and game engine tool development.

---

## Features

### Automatic Chain Detection
- Select a root and end joint
- Automatically traverses the hierarchy
- Builds a complete joint chain for rig generation

---

### Procedural Control Generation
- Generates control objects for each joint
- Creates offset groups for clean animation hierarchy
- Provides animator-friendly control structures instead of raw joints

---

### IK / FK Solver (FABRIK)
- Implements a FABRIK inverse kinematics solver
- Supports blending between IK and FK modes
- Mimics production-style rig behavior used in real pipelines

---

### Procedural Motion Systems
- Wave motion with per-joint phase offset (spine/tail ripple effect)
- Follow lag system for smooth secondary motion
- Spring dynamics for bounce, damping, and natural settling behavior

---

## Technical Highlights

- Built in Unity using C#
- Object-oriented modular rig architecture
- Procedural animation systems
- Custom IK solver implementation (FABRIK)
- Runtime rig generation and control binding
- Debugging of simulation stability (NaN handling, timing issues, solver order)

---

## Why I Built This

In production pipelines, tools like this exist inside DCC software (e.g., Maya) or proprietary engines to speed up rigging workflows.

The goal of this project was to simulate that environment inside Unity — allowing rigs to be generated procedurally in seconds instead of manual setup taking hours.

This project helped me understand:
- how animation systems are structured in production
- how IK/FK blending works under the hood
- how procedural systems interact with physics and transform hierarchies
- real-world debugging challenges in animation tools

---

## Known Issues / Limitations

- Prototype stage (not production-ready)
- Stability issues under extreme joint counts
- Some solver edge cases may require tuning
- Designed for experimentation, not final pipeline use

---

## Future Improvements

- UI tool panel for rig configuration
- Support for multiple IK chains
- Improved solver stability and performance optimization
- Exportable rig presets
- Integration with animation state systems

---

## Demo


https://github.com/user-attachments/assets/94b175f5-5023-446f-a42b-d7f8c3a32dea





---

## Author

**Saloni Kamboj**  
Software Engineering Graduate  
Unity / Backend / Systems & Tools Developer
