# Diagen_Unity_Beta
Beta integration of the Diagen system in Unity — dynamic NPC dialogue generation powered by local or remote LLMs.  This repository contains the beta version of the Diagen Unity plugin, a tool designed to streamline interactive writing and conditional dialogue generation in games.

Find a general demo here : https://www.youtube.com/watch?v=SZGEHh1zVYI&t=3s

# Diagen_Unity_Beta

**Beta version of the Diagen plugin for Unity** — a tool for dynamic, AI-powered NPC dialogue generation using local or remote LLMs.

## 🧰 Requirements

- **Unity version:** 2021.3.38f1 or later  
- **Dependencies:** [JSON .NET for Unity](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347) (available on the Unity Asset Store)


## 🛠️ Using the Diagen Editor

To open the custom editor interface:  
**Window → Diagen Editor**

Add your API Key in the corresponding section : Without your API key, this project will not work. Contact us at contact@xandimmersion.com if you have lost your API Key.

## 📦 Project Structure

- **Plugin folder:**  
  `Assets/Diagen/`  
  This is where the core Diagen system is located.

- **Demo scene:**  
  Open the test scene from:  
  `Assets/Diagen/Demo/FlowerIsland/`

- **NPC data tables:**  
  All character data tables are located in:  
  `Assets/Diagen/Demo/DemoDatatables/`  
  You can edit these tables directly to modify NPC behavior.  
  > ⚠️ If you add new tags, don’t forget to assign them to the character to make them active at the start of the game.

- **Demo scripts:**  
  Scripts used in the demo (and showcasing Diagen’s key functions) are in:  
  `Assets/Diagen/Demo/DemoScripts/`


