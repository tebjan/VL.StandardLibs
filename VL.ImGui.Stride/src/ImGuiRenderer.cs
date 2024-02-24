﻿using ImGuiNET;
using Stride.Input;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;
using RendererBase = Stride.Rendering.RendererBase;

using VL.Core;
using VL.Lib.Collections;
using VL.Stride;
using VL.Stride.Rendering;
using VL.Stride.Input;

using System.Reactive.Disposables;
using VL.Lib.Basics.Resources;
using System.Runtime.InteropServices;


namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;
    using SkiaRenderer = VL.Stride.SkiaRenderer;

    public partial class ImGuiRenderer : RendererBase, IDisposable
    {
        const int INITIAL_VERTEX_BUFFER_SIZE = 128;
        const int INITIAL_INDEX_BUFFER_SIZE = 128;

        // dependencies
        private readonly IResourceHandle<GraphicsDevice> deviceHandle;
        private readonly IResourceHandle<GraphicsContext> GraphicsContextHandle;
        private readonly IResourceHandle<InputManager> inputHandle;

        GraphicsDevice device => deviceHandle.Resource;
        GraphicsContext graphicsContext => GraphicsContextHandle.Resource;
        InputManager input => inputHandle.Resource;


        CommandList commandList;
        

        // ImGui
        private readonly ImGuiIOPtr _io;
        private readonly StrideContext _context;
        private ImDrawDataPtr _drawDataPtr;
        private float _fontScaling;
        private float _uiScaling;

        private Widget? widget;
        private WidgetLabel widgetLabel = new();
        private bool dockingEnabled;
        private Spread<FontConfig?> fonts = Spread.Create(FontConfig.Default);
        private bool fullscreenWindow;
        private IStyle? style;

        private GCHandle renderLayerHandle;
        private GCHandle textureHandle;


        // Stride
        private PipelineState imPipeline;
        private VertexDeclaration imVertLayout;
        private VertexBufferBinding vertexBinding;
        private IndexBufferBinding indexBinding;
        private readonly CustomDrawEffect imShader;
        private Texture? fontTexture;
        private Matrix projMatrix;

        private IInputSource? lastInputSource;
        private readonly SerialDisposable inputSubscription = new SerialDisposable();

        //Skia 
        private readonly SkiaRenderer skiaRenderer;

        //VL 
        NodeContext nodeContext;


        public unsafe ImGuiRenderer(CustomDrawEffect drawEffect, NodeContext nodeContext)
        {
            this.nodeContext = nodeContext;

            skiaRenderer = new SkiaRenderer();
            skiaRenderer.Space = VL.Skia.CommonSpace.DIPTopLeft;

            deviceHandle = AppHost.Current.Services.GetDeviceHandle();
            GraphicsContextHandle = AppHost.Current.Services.GetGraphicsContextHandle();
            inputHandle = AppHost.Current.Services.GetInputManagerHandle();
            imShader = drawEffect;


            _context = new StrideContext();
            using (_context.MakeCurrent())
            {
                _io = ImGui.GetIO();
                _io.NativePtr->IniFilename = null;

                #region DeviceObjects
                // set up a commandlist
                commandList = graphicsContext.CommandList;

                // set VertexLayout
                var layout = new VertexDeclaration(
                    VertexElement.Position<Vector2>(),
                    VertexElement.TextureCoordinate<Vector2>(),
                    VertexElement.Color(PixelFormat.R8G8B8A8_UNorm)
                );
                imVertLayout = layout;

                // pipeline desc
                var pipeline = new PipelineStateDescription()
                {
                    BlendState = BlendStates.NonPremultiplied,

                    RasterizerState = new RasterizerStateDescription()
                    {
                        CullMode = CullMode.None,
                        DepthBias = 0,
                        FillMode = FillMode.Solid,
                        MultisampleAntiAliasLine = false,
                        ScissorTestEnable = true,
                        SlopeScaleDepthBias = 0,
                    },

                    PrimitiveType = PrimitiveType.TriangleList,
                    InputElements = imVertLayout.CreateInputElements(),
                    DepthStencilState = DepthStencilStates.Default,

                    EffectBytecode = imShader.EffectInstance.Effect.Bytecode,
                    RootSignature = imShader.EffectInstance.RootSignature,

                    Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
                };


                // finally set up the pipeline
                var pipelineState = PipelineState.New(device, ref pipeline);
                imPipeline = pipelineState;


                // Setup Buffers
                var is32Bits = false;
                var indexBuffer = Buffer.Index.New(device, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Default);
                var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
                indexBinding = indexBufferBinding;

                var vertexBuffer = Buffer.Vertex.New(device, INITIAL_VERTEX_BUFFER_SIZE * imVertLayout.CalculateSize(), GraphicsResourceUsage.Default);
                var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, layout, 0);
                vertexBinding = vertexBufferBinding;
                #endregion DeviceObjects

                var scaling = VL.UI.Core.DIPHelpers.DIPFactor();
                UpdateScaling(fontScaling: scaling, uiScaling: scaling);
            }
        }
        
        #region scaling
        void UpdateScaling(float fontScaling, float uiScaling)
        {
            if (fontScaling != _fontScaling)
            {
                _fontScaling = fontScaling;
                BuildImFontAtlas(_io.Fonts, fontScaling);
            }
            if (uiScaling != _uiScaling)
            {
                _uiScaling = uiScaling;
                RenderHelper.UpdateUIScaling(uiScaling);
            }
        }
        #endregion scaling


        // need to be called from VL
        public void Update(Widget? widget, bool dockingEnabled, Spread<FontConfig?> fonts, bool fullscreenWindow, IStyle style)
        {
            this.widget = widget;
            this.dockingEnabled = dockingEnabled;

            if (!fonts.IsEmpty && !this.fonts.SequenceEqual(fonts))
            {
                this.fonts = fonts;
                BuildImFontAtlas(_io.Fonts, _fontScaling);
            }

            this.fullscreenWindow = fullscreenWindow;
            this.style = style;
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var commandList = context.CommandList;
            var renderTarget = commandList.RenderTarget;

            using (_context.MakeCurrent())
            {
                _io.DisplaySize = new System.Numerics.Vector2(renderTarget.Width, renderTarget.Height);
                _io.DisplayFramebufferScale = new System.Numerics.Vector2(1.0f, 1.0f);
                _io.DeltaTime = (float)context.RenderContext.Time.TimePerFrame.TotalSeconds;
                
                projMatrix = Matrix.OrthoRH(renderTarget.Width, -renderTarget.Height, -1, 1);

                var inputSource = context.RenderContext.GetWindowInputSource();
                if (inputSource != lastInputSource)
                {
                    lastInputSource = inputSource;
                    inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
                }

                // Enable Docking
                if (dockingEnabled)
                    _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

                _context.NewFrame();

                try
                {
                    using var _ = _context.ApplyStyle(style);

                    if (fullscreenWindow)
                    {
                        var viewPort = ImGui.GetMainViewport();
                        ImGui.SetNextWindowPos(viewPort.WorkPos);
                        ImGui.SetNextWindowSize(viewPort.WorkSize);
                        ImGui.Begin(widgetLabel.Update(null),
                            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration |
                            ImGuiWindowFlags.NoBackground);
                    }

                    // Enable Docking
                    if (dockingEnabled)
                    {
                        ImGui.DockSpaceOverViewport();
                    }

                    _context.SetDrawList(DrawList.AtCursor);
                    _context.Update(widget);
                }
                finally
                {
                    if (dockingEnabled)
                    {
                        ImGui.End();
                    }

                    if (fullscreenWindow)
                    {
                        ImGui.End();
                    }

                    // Render (builds mesh with texture coordinates)
                    ImGui.Render();
                }

                // Render the mesh
                _drawDataPtr = ImGui.GetDrawData();
            }

            imShader.SetParameters(context.RenderContext.RenderView, context);
            RenderDrawLists(context, _drawDataPtr);
        }

        private readonly CompositeDisposable errorImGuiInsideImGui = new CompositeDisposable();
        internal void ErrorImGuiInsideImGui()
        {
            if (IVLRuntime.Current != null)
            {
                if (errorImGuiInsideImGui.Count == 0)
                {
                    foreach (var id in nodeContext.Path.Stack.SkipLast(1))
                    {
                        errorImGuiInsideImGui.Add(IVLRuntime.Current.AddPersistentMessage(new Lang.Message(id, Lang.MessageSeverity.Error, "Don't use ImGui[Renderer] inside ImGui[Renderer]")));
                    }
                } 
            }   
        }

        protected override void Destroy()
        {
            errorImGuiInsideImGui.Dispose(); ;

            skiaRenderer.Dispose();

            imPipeline.Dispose();
            vertexBinding.Buffer.Dispose();
            indexBinding.Buffer.Dispose();
            fontTexture?.Dispose();
            imShader.Dispose();
            inputSubscription.Dispose();

            deviceHandle.Dispose();
            GraphicsContextHandle.Dispose();
            inputHandle.Dispose();

            renderLayerHandle.Free();
            textureHandle.Free();

            base.Destroy();
        }
    }
}
