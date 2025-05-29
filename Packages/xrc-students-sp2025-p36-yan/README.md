# XRC Sticky Note

A lightweight Unity package that provides sticky note functionality designed to work with Logitech MX Ink.

## Overview

TODO: Add overview

## Features

TODO: Add features

## Installation

TODO: Add installation instructions

## Usage

Documentation coming soon.

## Requirements

- Unity 6000.0.35f1 or newer
- Logitech MX Ink stylus

## Troubleshooting & Known Issues

### Android Manifest Configuration for Unity 6

When running this project on Android using Unity 6, the Android Manifest requires specific configuration. The Unity Player Activity needs to be changed from `UnityPlayerActivity` to `UnityPlayerGameActivity`.

You can update the Android Manifest using one of these methods:

1. Use Meta tools:

   - Navigate to `Meta > Tools > Update AndroidManiest.xml` or
   - Navigate to `Meta > Tools > Create store-compatible AndroidManiest.xml`

2. Manual update:
   - Change `android:name="com.unity3d.player.UnityPlayerActivity"` to `android:name="com.unity3d.player.UnityPlayerGameActivity"`

This configuration is necessary for the Unity 6 runtime.
