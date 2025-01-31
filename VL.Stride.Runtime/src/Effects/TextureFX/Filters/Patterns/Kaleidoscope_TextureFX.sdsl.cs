﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Stride Shader Mixin Code Generator.
// To generate it yourself, please install Stride.VisualStudio.Package .vsix
// and re-save the associated .sdfx.
// </auto-generated>

using System;
using Stride.Core;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Shaders;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering
{
    public static partial class Kaleidoscope_TextureFXKeys
    {
        public static readonly ValueParameterKey<int> Divisions = ParameterKeys.NewValue<int>(3);
        public static readonly ValueParameterKey<int> Iterations = ParameterKeys.NewValue<int>(5);
        public static readonly ValueParameterKey<float> IterationZoom = ParameterKeys.NewValue<float>(0.0f);
        public static readonly ValueParameterKey<float> Rotation = ParameterKeys.NewValue<float>(0.0f);
        public static readonly ValueParameterKey<float> Zoom = ParameterKeys.NewValue<float>(0.5f);
        public static readonly ValueParameterKey<Vector2> Center = ParameterKeys.NewValue<Vector2>(new Vector2(0.0f,0.0f));
        public static readonly ValueParameterKey<Vector2> CellOffset = ParameterKeys.NewValue<Vector2>(new Vector2(0.0f,0.0f));
        public static readonly ValueParameterKey<float> CellRotation = ParameterKeys.NewValue<float>(0.0f);
        public static readonly ValueParameterKey<Vector2> CellScale = ParameterKeys.NewValue<Vector2>(new Vector2(1.0f,1.0f));
        public static readonly ValueParameterKey<Vector4> ControlFactor = ParameterKeys.NewValue<Vector4>(new Vector4(1.0f,0.0f,0.0f,0.0f));
        public static readonly ValueParameterKey<bool> Aspect = ParameterKeys.NewValue<bool>(true);
        public static readonly ObjectParameterKey<SamplerState> s0 = ParameterKeys.NewObject<SamplerState>();
    }
}
