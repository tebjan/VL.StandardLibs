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
    public static partial class Mandelbrot_TextureFXKeys
    {
        public static readonly ValueParameterKey<Matrix> Transform = ParameterKeys.NewValue<Matrix>();
        public static readonly ValueParameterKey<Color4> BackgroundColor = ParameterKeys.NewValue<Color4>(new Color4(0.0f,0.0f,0.0f,1.0f));
        public static readonly ValueParameterKey<Color4> Color = ParameterKeys.NewValue<Color4>(new Color4(1.0f,1.0f,1.0f,1.0f));
        public static readonly ValueParameterKey<int> Iterations = ParameterKeys.NewValue<int>(16);
        public static readonly ValueParameterKey<Vector2> Control = ParameterKeys.NewValue<Vector2>(new Vector2(0.35f,0.35f));
        public static readonly ValueParameterKey<float> Zoom = ParameterKeys.NewValue<float>(-0.5f);
        public static readonly ValueParameterKey<float> Morph = ParameterKeys.NewValue<float>(1.0f);
        public static readonly ValueParameterKey<bool> Aspect = ParameterKeys.NewValue<bool>(true);
    }
}
