# Mesh Growth Simulation

This repository documents my learning from Long Nguyen's workshop on **C# Scripting and Plugin Development for Grasshopper(https://www.youtube.com/watch?v=pFCrIzENDn8&list=PLapoQ_9M-ujfYGOsZProIXPGx-HRfjJ9C&index=1)**. 
It focuses on re-implementing the workshop's mesh growth algorithms within Grasshopper, using C# scripting (Mac) and developing a custom component/plugin (Windows).

---

## Background

- **RhinoCommon** is a .NET library, so all code for plugin or component development is written in **C#**.  
- The workshop uses **Windows + Visual Studio** because Visual Studio offers a more complete ecosystem for C# development, including building `.gha` or `.dll` plugins.  
- On **Mac**, Visual Studio no longer fully supports C# plugin development. While **VS Code** can be used to write C# code, it cannot directly compile `.gha` or `.dll` files for Grasshopper.  

---

## Approach on Mac

Even though I cannot directly develop custom Grasshopper plugins on Mac, I wanted to practice writing **C# mesh growth scripts** more conveniently.  
- The **native C# component editor** in Grasshopper is very limited and hard to use.  
- I used the **ScriptParasite** plugin (tutorial from ParametricCamp(https://www.youtube.com/watch?v=m-Mf34CvTX4&list=PLx3k0RGeXZ_yZgg-f2k7fO3WxBQ0zLCeU&index=28)) to sync C# code written in **VS Code** to the C# component.  
- This setup allows:  
  - Intuitive code editing with **VS Code auto-completion**.  
  - Real-time syncing to Grasshopper components for testing.  

> This setup is purely for learning and experimenting with mesh growth algorithms in Grasshopper on a Mac.

---

## Repository Structure

The repo contains two main folders:

1. **For Mac**  
   - Contains **C# scripting code for mesh growth algorithms**.  
   - Written in VS Code and synced to Grasshopper components via ScriptParasite.  

2. **For Windows**  
   - Contains **C# scripting code for mesh growth algorithms** (with ScriptParasite).  
   - Also includes **C# code for developing custom mesh growth components/plugins**, which can be compiled into `.gha` or `.dll` using Visual Studio.

---

## Notes

- This repository is primarily an **exercise in learning C# scripting in Grasshopper**.  
- Mac users can explore and run the scripts using ScriptParasite + VS Code setup.  
- Windows users can additionally experiment with building **custom plugins**.

---

## References

- Long Nguyen’s workshop: [YouTube Playlist]([https://www.youtube.com/](https://www.youtube.com/watch?v=pFCrIzENDn8&list=PLapoQ_9M-ujfYGOsZProIXPGx-HRfjJ9C&index=1))  
- Parametric Camp tutorial on ScriptParasite: [YouTube Video]([https://www.youtube.com/](https://youtu.be/m-Mf34CvTX4?si=PwbleTiYMi-r-puh))

---


