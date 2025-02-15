# spludlow-tetris
A network implementation of Tetris allowing multiple players connected to a server

## Introduction
I built a Tetris game on my Amiga as a kid. I wanted to see what's needed to do it on Windows.

This code demonstrates the following:

- Tetris Logic
- Bot Logic to play
- Play Sounds using `SharpDX.XAudio2`
- Joystick Input using `SharpDX.DirectInput`
- Proprietary Network Game Protocol using `System.Net.Sockets`
- Server using Threads `System.Threading`
