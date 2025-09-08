# Filamented Standard
A transposal of the shaders from Filament over the framework of Unity's Standard shader.

*Filament is a real-time physically based rendering engine for Android, iOS, Linux, macOS, Windows,
and WebGL. It is designed to be as small as possible and as efficient as possible on Android.*

This repo is based on Filament v1.9.23.

It supports more shading models than Standard and follows a less rigid structure. This project's goal is to combine the familiar interface of the Standard shader with a shading model that's more up to date and less idiosyncratic. 

## Installation

Download the repository, and place the folder with the package.json into your Unity project's Packages/ directory. This will install Filamented as a local package.

## Usage

Filamented is provided as a main shader and a set of additional extras. With the main Filamented shader, all the functionality and options of Unity Standard are there. You can switch a shader from Standard to Filamented and all the properties and settings will be transferred over with no losses. 

There is also a tool provided for automatically swapping over all Materials in the current scene to Filamented, provided in the Tools menu. By default, it will swap from Standard to Filamented for the metalness workflow, but it will work for any other combination of shaders and can be used to swap from Standard (Specular setup) or Autodesk Interactive to Filamented (Specular setup) or Filamented (Roughness setup) respectively. 

Filamented also comes with some extras. These alternate versions of Filamented offer different options or are provided as a base for your own edits. 

For example, Filamented Template is a basic example of how Filamented can be customized - it packs all the material properties together into a single texture, and has fewer shader variants than Standard.

## Project Details

This project's goal is to combine the familiar interface of the Standard shader with a shading model that's more up to date and less idiosyncratic from Filament.

For example, non-metallic materials in Standard have a Fresnel reflection that is too strong, due to an imprecise approximation of the Fresnel effect. Filamented uses a modern and more correct way of handling the specular calculations, giving glossy surfaces a softer Fresnel shine that appears more natural. 

In worlds with baked lighting, this can be combined with Exposure Occlusion which allows the shadows baked in lightmaps to occlude specular reflections and remove strange inaccurate reflections. 

What's Filament? Filament is a real-time physically based rendering engine for Android, iOS, Linux, macOS, Windows, and WebGL. It is designed to be as small as possible and as efficient as possible on Android.

## License?
Licensed under the Apache license. 

Filament code is included under the Apache license. 
Copyright (C) 2020 Google, Inc.

Some Unity files may additionally be covered by the MIT license.
Copyright (c) 2016 Unity Technologies.