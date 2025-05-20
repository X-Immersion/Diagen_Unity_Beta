# Features

**[← Table of contents](../README.md#table-of-contents)**

### On this page

[🎚️ Audio effects](#-audio-effects)<br/>
[📝 Tags](#-tags)<br/>

## 🎚️ Audio effects

When generating audio speech with the Ariel plugin, you can apply audio effects to the generated audio. Those effects are optional and can be combined to create the desired sound atmosphere. Here is a list of all audio effects available:

| Name              | Description |
| ----------------- | ----------- |
| **Telephone**     | The voice sounds like it's coming from a phone |
| **Cave**          | The voice sounds like the speaker is in a cave |
| **Small cave**    | The voice sounds like the speaker is in a small cave |
| **Gas mask**      | The voice sounds like the speaker has a gas mask |
| **Bad reception** | The voice sounds like it's coming from a phone with a bad reception |
| **Next room**     | The voice sounds like the speaker is in the next room |
| **Alien**         | An alien audio effect is added to the voice |
| **Alien 2 (alt)** | An other alien audio effect (like in the space) is added to the voice |
| **Stereo**        | The audio file have two channels (the mono channel is duplicated) |

> [!TIP]
> the **Alien** effect is automatically applied to the voices *Xalith*, as well as the **Alien2** effect is automatically applied to the voices *Zephyr* and *Yorgon*. They cannot be removed.

## 📝 Tags

There are two types of tags that can be used:

- [Pause Tags](#pause-tags)
- [Emotion Tags](#emotion-tags)


**Tags availability:** The tag system is currently only supported on the Remote version of Ariel.

### Pause Tags

In your sentence, you can enter a silence tag for a custom pause. Write `<pause Xs>` or `<pause Xms>` where ***X*** is the duration (in seconds or milliseconds). For example:

`Hi, how are you? <pause 3s> My name is Jane.`

### Emotion Tags

Emotion tags can be applied to modify the tone of the generated speech. You can use tags like `<emotion happy>`, `<emotion sad>`, etc. For example:

`This is amazing! <emotion happy> I'm thrilled!`

The emotion tags are active for the generated text until the next emotion tag in the current text. If no emotion tag is found, the default emotion is neutral. For example:

`I'm feeling great today!<emotion happy> Let’s celebrate! <emotion angry>I'm so mad! Go away.`

In this example the first sentence will be generated with a neutral emotion, the second sentence with an happy emotion, and the last two sentences with an angry emotion.

**Emotion Availability:** Not every speaker has emotions available. If the speaker does not support emotions, the emotion tags will be ignored and the default emotion is used. To check which speakers support emotions, please refer to the [X&Immersion Create APP](https://create.xandimmersion.com/).

### Tag Examples

To help you understand how each tag works, here are a few examples:
- For pauses: `This is a test.<pause 2s> Please wait.`
- For emotions: `I'm feeling great today!<emotion happy> Let’s celebrate!`

<br/>
<br/>