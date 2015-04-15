﻿/******************************************************************************
 * Spine Runtimes Software License
 * Version 2.1
 * 
 * Copyright (c) 2013, Esoteric Software
 * All rights reserved.
 * 
 * You are granted a perpetual, non-exclusive, non-sublicensable and
 * non-transferable license to install, execute and perform the Spine Runtimes
 * Software (the "Software") solely for internal use. Without the written
 * permission of Esoteric Software (typically granted by licensing Spine), you
 * may not (a) modify, translate, adapt or otherwise create derivative works,
 * improvements of the Software or develop new applications using the Software
 * or (b) remove, delete, alter or obscure any trademarks or any copyright,
 * trademark, patent or other intellectual property or proprietary rights
 * notices on or in the Software, including any copy thereof. Redistributions
 * in binary or source form must include this license and terms.
 * 
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE "AS IS" AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL ESOTERIC SOFTARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Spine {
	/// <summary>Draws region and mesh attachments.</summary>
	public class SkeletonMeshRenderer {
		private const int TL = 0;
		private const int TR = 1;
		private const int BL = 2;
		private const int BR = 3;

		GraphicsDevice device;
		MeshBatcher batcher;
		RasterizerState rasterizerState;
		float[] vertices = new float[8];
		int[] quadTriangles = { 0, 1, 2, 1, 3, 2 };
		BlendState defaultBlendState;

		BasicEffect effect;
		public BasicEffect Effect { get { return effect; } set { effect = value; } }

        // JLS: Added these accessors so we can do our own Begin/End with a custom shader.
        public MeshBatcher Batcher { get { return batcher; } } 
        public BlendState DefaultBlendState { get { return defaultBlendState; } set { defaultBlendState = value; } }

		private bool premultipliedAlpha;
		public bool PremultipliedAlpha { get { return premultipliedAlpha; } set { premultipliedAlpha = value; } }

		public SkeletonMeshRenderer (GraphicsDevice device) {
			this.device = device;

			batcher = new MeshBatcher();

			effect = new BasicEffect(device);
			effect.World = Matrix.Identity;
			effect.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
			effect.TextureEnabled = true;
			effect.VertexColorEnabled = true;

			rasterizerState = new RasterizerState();
			rasterizerState.CullMode = CullMode.None;

			Bone.yDown = true;
		}

		public void Begin () {
			defaultBlendState = premultipliedAlpha ? BlendState.AlphaBlend : BlendState.NonPremultiplied;

			device.RasterizerState = rasterizerState;
			device.BlendState = defaultBlendState;

			effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 1, 0);
		}

		public void End () {
			foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
				pass.Apply();
				batcher.Draw(device);
			}
		}

		public void Draw (Skeleton skeleton) {
			float[] vertices = this.vertices;
			List<Slot> drawOrder = skeleton.DrawOrder;
			float skeletonR = skeleton.R, skeletonG = skeleton.G, skeletonB = skeleton.B, skeletonA = skeleton.A;
			for (int i = 0, n = drawOrder.Count; i < n; i++) {
				Slot slot = drawOrder[i];
				Attachment attachment = slot.Attachment;
				if (attachment is RegionAttachment) {
					RegionAttachment regionAttachment = (RegionAttachment)attachment;
					BlendState blend = slot.Data.AdditiveBlending ? BlendState.Additive : defaultBlendState;
					if (device.BlendState != blend) {
						End();
						device.BlendState = blend;
					}

					MeshItem item = batcher.NextItem(4, 6);
					item.triangles = quadTriangles;
					VertexPositionColorTexture[] itemVertices = item.vertices;

					AtlasRegion region = (AtlasRegion)regionAttachment.RendererObject;
					item.texture = (Texture2D)region.page.rendererObject;

					Color color;
					float a = skeletonA * slot.A * regionAttachment.A;
					if (premultipliedAlpha) {
						color = new Color(
								skeletonR * slot.R * regionAttachment.R * a,
								skeletonG * slot.G * regionAttachment.G * a,
								skeletonB * slot.B * regionAttachment.B * a, a);
					} else {
						color = new Color(
								skeletonR * slot.R * regionAttachment.R,
								skeletonG * slot.G * regionAttachment.G,
								skeletonB * slot.B * regionAttachment.B, a);
					}
					itemVertices[TL].Color = color;
					itemVertices[BL].Color = color;
					itemVertices[BR].Color = color;
					itemVertices[TR].Color = color;

					regionAttachment.ComputeWorldVertices(slot.Bone, vertices);
					itemVertices[TL].Position.X = vertices[RegionAttachment.X1];
					itemVertices[TL].Position.Y = vertices[RegionAttachment.Y1];
					itemVertices[TL].Position.Z = 0;
					itemVertices[BL].Position.X = vertices[RegionAttachment.X2];
					itemVertices[BL].Position.Y = vertices[RegionAttachment.Y2];
					itemVertices[BL].Position.Z = 0;
					itemVertices[BR].Position.X = vertices[RegionAttachment.X3];
					itemVertices[BR].Position.Y = vertices[RegionAttachment.Y3];
					itemVertices[BR].Position.Z = 0;
					itemVertices[TR].Position.X = vertices[RegionAttachment.X4];
					itemVertices[TR].Position.Y = vertices[RegionAttachment.Y4];
					itemVertices[TR].Position.Z = 0;

					float[] uvs = regionAttachment.UVs;
					itemVertices[TL].TextureCoordinate.X = uvs[RegionAttachment.X1];
					itemVertices[TL].TextureCoordinate.Y = uvs[RegionAttachment.Y1];
					itemVertices[BL].TextureCoordinate.X = uvs[RegionAttachment.X2];
					itemVertices[BL].TextureCoordinate.Y = uvs[RegionAttachment.Y2];
					itemVertices[BR].TextureCoordinate.X = uvs[RegionAttachment.X3];
					itemVertices[BR].TextureCoordinate.Y = uvs[RegionAttachment.Y3];
					itemVertices[TR].TextureCoordinate.X = uvs[RegionAttachment.X4];
					itemVertices[TR].TextureCoordinate.Y = uvs[RegionAttachment.Y4];
				} else if (attachment is MeshAttachment) {
					MeshAttachment mesh = (MeshAttachment)attachment;
					int vertexCount = mesh.Vertices.Length;
					if (vertices.Length < vertexCount) vertices = new float[vertexCount];
					mesh.ComputeWorldVertices(slot, vertices);

					int[] triangles = mesh.Triangles;
					MeshItem item = batcher.NextItem(vertexCount, triangles.Length);
					item.triangles = triangles;

					AtlasRegion region = (AtlasRegion)mesh.RendererObject;
					item.texture = (Texture2D)region.page.rendererObject;

					Color color;
					float a = skeletonA * slot.A * mesh.A;
					if (premultipliedAlpha) {
						color = new Color(
								skeletonR * slot.R * mesh.R * a,
								skeletonG * slot.G * mesh.G * a,
								skeletonB * slot.B * mesh.B * a, a);
					} else {
						color = new Color(
								skeletonR * slot.R * mesh.R,
								skeletonG * slot.G * mesh.G,
								skeletonB * slot.B * mesh.B, a);
					}

					float[] uvs = mesh.UVs;
					VertexPositionColorTexture[] itemVertices = item.vertices;
					for (int ii = 0, v = 0; v < vertexCount; ii++, v += 2) {
						itemVertices[ii].Color = color;
						itemVertices[ii].Position.X = vertices[v];
						itemVertices[ii].Position.Y = vertices[v + 1];
						itemVertices[ii].Position.Z = 0;
						itemVertices[ii].TextureCoordinate.X = uvs[v];
						itemVertices[ii].TextureCoordinate.Y = uvs[v + 1];
					}
				} else if (attachment is SkinnedMeshAttachment) {
					SkinnedMeshAttachment mesh = (SkinnedMeshAttachment)attachment;
					int vertexCount = mesh.UVs.Length;
					if (vertices.Length < vertexCount) vertices = new float[vertexCount];
					mesh.ComputeWorldVertices(slot, vertices);

					int[] triangles = mesh.Triangles;
					MeshItem item = batcher.NextItem(vertexCount, triangles.Length);
					item.triangles = triangles;

					AtlasRegion region = (AtlasRegion)mesh.RendererObject;
					item.texture = (Texture2D)region.page.rendererObject;

					Color color;
					float a = skeletonA * slot.A * mesh.A;
					if (premultipliedAlpha) {
						color = new Color(
								skeletonR * slot.R * mesh.R * a,
								skeletonG * slot.G * mesh.G * a,
								skeletonB * slot.B * mesh.B * a, a);
					} else {
						color = new Color(
								skeletonR * slot.R * mesh.R,
								skeletonG * slot.G * mesh.G,
								skeletonB * slot.B * mesh.B, a);
					}

					float[] uvs = mesh.UVs;
					VertexPositionColorTexture[] itemVertices = item.vertices;
					for (int ii = 0, v = 0; v < vertexCount; ii++, v += 2) {
						itemVertices[ii].Color = color;
						itemVertices[ii].Position.X = vertices[v];
						itemVertices[ii].Position.Y = vertices[v + 1];
						itemVertices[ii].Position.Z = 0;
						itemVertices[ii].TextureCoordinate.X = uvs[v];
						itemVertices[ii].TextureCoordinate.Y = uvs[v + 1];
					}
				}
			}
		}

        public Rectangle calcBoundingRect(Skeleton skeleton)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0.0f);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, 0.0f);

            List<Slot> drawOrder = skeleton.DrawOrder;
            for (int i = 0, n = drawOrder.Count; i < n; i++)
            {
                Slot slot = drawOrder[i];
                RegionAttachment regionAttachment = slot.Attachment as RegionAttachment;
                if (regionAttachment != null)
                {

                    float[] vertices = this.vertices;
                    regionAttachment.ComputeWorldVertices(slot.Bone, vertices);

                    Vector3 testVector = new Vector3(vertices[RegionAttachment.X1], vertices[RegionAttachment.Y1], 0.0f);
                    min = Vector3.Min(min, testVector);
                    max = Vector3.Max(max, testVector);

                    testVector = new Vector3(vertices[RegionAttachment.X2], vertices[RegionAttachment.Y2], 0.0f);
                    min = Vector3.Min(min, testVector);
                    max = Vector3.Max(max, testVector);

                    testVector = new Vector3(vertices[RegionAttachment.X3], vertices[RegionAttachment.Y3], 0.0f);
                    min = Vector3.Min(min, testVector);
                    max = Vector3.Max(max, testVector);

                    testVector = new Vector3(vertices[RegionAttachment.X4], vertices[RegionAttachment.Y4], 0.0f);
                    min = Vector3.Min(min, testVector);
                    max = Vector3.Max(max, testVector);
                }
            }

            BoundingBox box = new BoundingBox(min, max);

            return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        }


        public bool getRectForSlot(Skeleton skeleton, string SlotName, out Rectangle resultRect)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0.0f);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, 0.0f);

            List<Slot> drawOrder = skeleton.DrawOrder;
            for (int i = 0, n = drawOrder.Count; i < n; i++)
            {
                Slot slot = drawOrder[i];
                if (slot.ToString() == SlotName)
                {
                    RegionAttachment regionAttachment = slot.Attachment as RegionAttachment;
                    if (regionAttachment != null)
                    {

                        float[] vertices = this.vertices;
                        regionAttachment.ComputeWorldVertices(slot.Bone, vertices);

                        Vector3 testVector = new Vector3(vertices[RegionAttachment.X1], vertices[RegionAttachment.Y1], 0.0f);
                        min = Vector3.Min(min, testVector);
                        max = Vector3.Max(max, testVector);

                        testVector = new Vector3(vertices[RegionAttachment.X2], vertices[RegionAttachment.Y2], 0.0f);
                        min = Vector3.Min(min, testVector);
                        max = Vector3.Max(max, testVector);

                        testVector = new Vector3(vertices[RegionAttachment.X3], vertices[RegionAttachment.Y3], 0.0f);
                        min = Vector3.Min(min, testVector);
                        max = Vector3.Max(max, testVector);

                        testVector = new Vector3(vertices[RegionAttachment.X4], vertices[RegionAttachment.Y4], 0.0f);
                        min = Vector3.Min(min, testVector);
                        max = Vector3.Max(max, testVector);

                        resultRect = new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
                        return true;

                    }
                }
            }

            resultRect = new Rectangle();
            return false;
        }
	}
}
