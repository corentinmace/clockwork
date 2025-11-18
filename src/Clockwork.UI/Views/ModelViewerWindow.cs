using System;
using ImGuiNET;
using Clockwork.Core.Formats.NDS.NSBMD;
using Clockwork.Core.Formats.NDS.MapFile;
using OpenTK.Graphics.OpenGL;

namespace Clockwork.UI.Views
{
    /// <summary>
    /// 3D Model Viewer window for NSBMD models
    /// </summary>
    public class ModelViewerWindow
    {
        private NSBMDGlRenderer renderer;
        private NSBMD currentModel;
        private float rotationAngle = 0.0f;
        private float distance = 10.0f;
        private float elevation = 45.0f;
        private bool showWireframe = false;

        public ModelViewerWindow()
        {
            renderer = new NSBMDGlRenderer();
        }

        /// <summary>
        /// Load a MapFile and display its 3D model
        /// </summary>
        public void LoadMapFile(string path)
        {
            try
            {
                var mapFile = new MapFile.MapFile(path);
                if (mapFile.mapModel != null)
                {
                    currentModel = mapFile.mapModel;
                    // TODO: Setup renderer with the model
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map file: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a standalone NSBMD file
        /// </summary>
        public void LoadNSBMD(string path)
        {
            try
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    currentModel = NSBMDLoader.LoadNSBMD(stream);
                    // TODO: Setup renderer with the model
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading NSBMD file: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw the ImGui window
        /// </summary>
        public void Draw()
        {
            if (ImGui.Begin("3D Model Viewer"))
            {
                // Controls
                ImGui.Text("Camera Controls:");
                ImGui.SliderFloat("Rotation", ref rotationAngle, 0.0f, 360.0f);
                ImGui.SliderFloat("Distance", ref distance, 1.0f, 50.0f);
                ImGui.SliderFloat("Elevation", ref elevation, -90.0f, 90.0f);
                ImGui.Checkbox("Wireframe", ref showWireframe);

                ImGui.Separator();

                // Model info
                if (currentModel != null)
                {
                    ImGui.Text($"Model loaded: {currentModel.models?.Length ?? 0} model(s)");
                    if (currentModel.models != null && currentModel.models.Length > 0)
                    {
                        var model = currentModel.models[0];
                        ImGui.Text($"Name: {model.Name ?? "Unknown"}");
                        ImGui.Text($"Materials: {model.Materials.Count}");
                        ImGui.Text($"Polygons: {model.Polygons.Count}");
                        ImGui.Text($"Objects: {model.Objects.Count}");
                    }
                }
                else
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                        "No model loaded. Use File > Load to load a model.");
                }

                ImGui.Separator();

                // 3D Viewport (placeholder)
                var viewportSize = ImGui.GetContentRegionAvail();
                if (viewportSize.X > 0 && viewportSize.Y > 0)
                {
                    // TODO: Render 3D model here using OpenGL
                    // For now, just show a colored rectangle as placeholder
                    var drawList = ImGui.GetWindowDrawList();
                    var pos = ImGui.GetCursorScreenPos();
                    drawList.AddRectFilled(pos,
                        new System.Numerics.Vector2(pos.X + viewportSize.X, pos.Y + viewportSize.Y),
                        ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.2f, 0.3f, 1.0f)));

                    drawList.AddText(
                        new System.Numerics.Vector2(pos.X + viewportSize.X / 2 - 50, pos.Y + viewportSize.Y / 2),
                        ImGui.GetColorU32(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
                        "3D Viewport");
                }
            }
            ImGui.End();
        }

        /// <summary>
        /// Render the 3D model using OpenGL
        /// </summary>
        private void RenderModel()
        {
            if (currentModel == null || renderer == null)
                return;

            // Setup camera
            GL.PushMatrix();
            GL.Rotate(elevation, 1.0f, 0.0f, 0.0f);
            GL.Rotate(rotationAngle, 0.0f, 1.0f, 0.0f);
            GL.Translate(0.0f, 0.0f, -distance);

            // Render wireframe or solid
            if (showWireframe)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }
            else
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }

            // TODO: Call renderer to draw the model
            // renderer.RenderModel(currentModel);

            GL.PopMatrix();
        }
    }
}
