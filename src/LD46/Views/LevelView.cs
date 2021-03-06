﻿using Floppy.Extensions;
using Floppy.Graphics;
using Floppy.Physics;
using Floppy.Utilities;
using LD46.Entities;
using LD46.Graphics;
using LD46.Levels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace LD46.Views {
    public class LevelView {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly IRenderTargetStack _renderTargetStack;
        private readonly WaterView _waterView;
        private readonly ParticleFactory _particleFactory;
        private readonly Effect _waterEffect;
        private readonly Texture2D _flowMapTexture, _pixelTexture, _arrowTexture, _finishTexture, _progressTexture,
            _playerIconTexture, _torchIconTexture, _gradientTexture;
        private readonly SpriteFont _regularFont;

        private RenderTarget2D _worldTarget, _waterTarget;

        private readonly Camera2D _camera = new Camera2D();

        private int _extraSize = 128;

        private float _shaderTimer = 0f;

        private bool _showLoseScreen = false;
        private float _loseScreenOpacity = 0f;

        private bool _fadeOut = false;
        private float _fadeOutOpacity = 0f;

        private readonly Sprite _arrowSprite;

        private readonly Random _random = new Random();

        private float _lightRadius = 0f;
        private float _flickerTimer = 0f;
        private const float _flickerTime = 0.075f;

        private float _ambience = 0.5f;
        private float _radius = 400f;

        private float _headerTimer = 0f;
        private const float _headerTime = 3f;

        public LevelView(GraphicsDevice graphicsDevice, ContentManager content,
            SpriteBatch spriteBatch, IRenderTargetStack renderTargetStack, 
            BackgroundView backgroundView, TileMapView tileMapView, EntitiesView entitiesView, 
            WaterView waterView, ParticlesView particlesView, ParticleFactory particleFactory) {

            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _renderTargetStack = renderTargetStack;
            Background = backgroundView;
            TileMap = tileMapView;
            Entities = entitiesView;
            Particles = particlesView;
            _particleFactory = particleFactory;
            _waterView = waterView;

            _waterEffect = content.Load<Effect>("Effects/Water");
            _flowMapTexture = content.Load<Texture2D>("Textures/FlowMap");
            _pixelTexture = content.Load<Texture2D>("Textures/Pixel");
            _arrowTexture = content.Load<Texture2D>("Textures/TorchArrow");
            _finishTexture = content.Load<Texture2D>("Textures/Finish");
            _progressTexture = content.Load<Texture2D>("Textures/Progress");
            _playerIconTexture = content.Load<Texture2D>("Textures/PlayerIcon");
            _torchIconTexture = content.Load<Texture2D>("Textures/TorchIcon");
            _gradientTexture = content.Load<Texture2D>("Textures/Gradient");
            _regularFont = content.Load<SpriteFont>("Fonts/Regular");

            _worldTarget = CreateRenderTarget();
            _waterTarget = CreateRenderTarget();

            _waterEffect.Parameters["WaterMaskSampler+WaterMask"].SetValue(_waterTarget);
            _waterEffect.Parameters["FlowMapSampler+FlowMap"].SetValue(_flowMapTexture);
            _waterEffect.Parameters["WaterColor"].SetValue(new Color(152, 163, 152).ToVector4());

            _arrowSprite = new Sprite(_arrowTexture) {
                Origin = _arrowTexture.Bounds.Center.ToVector2()
            };

            Entities.Particles = Particles;
        }

        public int TorchEntityID { get; set; }
        public int PlayerEntityID { get; set; }

        public EntitiesView Entities { get; }
        public ParticlesView Particles { get; }
        public TileMapView TileMap { get; }
        public BackgroundView Background { get; }

        public Vector2 Start { get; set; }

        public bool HideProgress { get; set; } = false;
        public bool TheWinnerIsYou { get; set; } = false;

        public bool HasFadedOut => _fadeOutOpacity >= 1f;

        public string? Header { get; set; }

        public void ShowLoseScreen() {
            _showLoseScreen = true;
        }

        public void FadeOut() {
            _fadeOut = true;
        }

        public void Update(Level level, float deltaTime) {
            _shaderTimer += deltaTime;

            if (_headerTimer < _headerTime) {
                _headerTimer += deltaTime;
            }

            Entities.Update(level, deltaTime);
            _waterView.Update(deltaTime);
            Particles.Update(deltaTime);

            if (_showLoseScreen) {
                _loseScreenOpacity += 0.3f * deltaTime;
                _loseScreenOpacity = MathHelper.Min(_loseScreenOpacity, 1f);
            }

            if (_fadeOut) {
                _fadeOutOpacity += 1f * deltaTime;
                _fadeOutOpacity = MathHelper.Min(_fadeOutOpacity, 1f);
            }

            _flickerTimer += deltaTime;
            if (_flickerTimer >= _flickerTime) {
                _flickerTimer -= _flickerTime;
                _lightRadius = _radius + (float)_random.NextDouble() * 20f;
            }

            for (int i = 0; i < level.WindChannels.Count; i++) {
                float channel = level.WindChannels[i];

                Particles.Add(_particleFactory.CreateWindParticle(
                    new Vector2((i % 2 == 0 ? -500f : 500f) + level.TileMap.Width * GraphicsConstants.TileSize * (float)_random.NextDouble(),
                        GraphicsConstants.PhysicsToView(channel) - GraphicsConstants.TileSize * 4f + (float)_random.NextDouble() * 8f * GraphicsConstants.TileSize),
                    new Vector2(i % 2 == 0 ? 1f : -1f, 0f)));
            }
        }

        public void SetLighting(float radius, float ambience) {
            _radius = radius;
            _ambience = ambience;
        }

        public void Draw(Level level) {
            if (RenderTargetsAreOutdated()) {
                UpdateRenderTargets();
            }

            _camera.Position = Vector2.Round(GraphicsConstants.PhysicsToView(level.CameraCenter) - _graphicsDevice.Viewport.Bounds.Size.ToVector2() / 2f);

            _renderTargetStack.Push(_worldTarget);
            {
                _graphicsDevice.Clear(Color.Transparent);
                Background.Draw(_camera);
                TileMap.Draw(level, _camera);

                if (HideProgress) {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetTransformMatrix());

                    _spriteBatch.DrawString(_regularFont, "Press SPACE BAR to jump/kick.", new Vector2(256f, 2048f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "Use the ARROW KEYS to move.", new Vector2(60f, 1700f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "Tap DOWN to fall through platforms.", new Vector2(140f, 1520f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "Strike R to restart.", new Vector2(670f, 1800f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "Slap SHIFT while running to dash.", new Vector2(200f, 1190f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "You can chain kicks and dashes...", new Vector2(80f, 900f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "... if you hit your torch.", new Vector2(300f, 800f), Color.White);
                    _spriteBatch.DrawString(_regularFont, "Psst... you can press P\nto skip this tutorial.", new Vector2(-350f, 2170f), Color.White);

                    _spriteBatch.End();
                }

                if (TheWinnerIsYou) {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetTransformMatrix());

                    _spriteBatch.DrawString(_regularFont, "You win! Sleep tight.", new Vector2(80f, 270f), Color.White);

                    _spriteBatch.End();
                }

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetTransformMatrix());

                int startX = (int)Math.Floor(_camera.Position.X / _finishTexture.Width);
                int endX = (int)Math.Floor((_camera.Position.X + _spriteBatch.GraphicsDevice.Viewport.Width) / _finishTexture.Width);

                float y = GraphicsConstants.PhysicsToView(level.FinishHeight);

                for (int x = startX; x <= endX; x++) {
                    _spriteBatch.Draw(_finishTexture, new Vector2(x * _finishTexture.Width, y - _finishTexture.Height / 2f), Color.White);
                }

                _spriteBatch.End();

                Particles.Draw(_camera);
                Entities.Draw(level, _camera);

                TileMap.DrawGrates(level, _camera);
            }
            _renderTargetStack.Pop();

            _renderTargetStack.Push(_waterTarget);
            {
                _graphicsDevice.Clear(Color.Transparent);
                _waterView.DrawMask(level, _camera);
            }
            _renderTargetStack.Pop();

            _waterEffect.Parameters["Time"].SetValue(_shaderTimer);

            Vector2 camera = _camera.Position / _graphicsDevice.Viewport.Bounds.Size.ToVector2();
            _waterEffect.Parameters["Camera"].SetValue(camera);

            _waterEffect.Parameters["Position"].SetValue(_camera.Position);
            _waterEffect.Parameters["Dimensions"].SetValue(_graphicsDevice.Viewport.Bounds.Size.ToVector2() + new Vector2(128f));

            if (level.EntityWorld.TryGetEntity(TorchEntityID, out Entity? te)) {
                if (!te.IsPutOut && level.PhysicsWorld.TryGetBody(te.BodyID, out Body? torchBody)) {
                    _waterEffect.Parameters["Light1"].SetValue(GraphicsConstants.PhysicsToView(torchBody.Position + torchBody.Bounds.Center));
                }
                else {
                    _waterEffect.Parameters["Light1"].SetValue(new Vector2(-1000f));
                }
            }

            _waterEffect.Parameters["Radius"].SetValue(_lightRadius);
            _waterEffect.Parameters["Ambience"].SetValue(_ambience);

            _spriteBatch.Begin(effect: _waterEffect);
            _spriteBatch.Draw(_worldTarget, Vector2.Zero, Color.White);
            _spriteBatch.End();

            _waterView.Draw(level, _camera);

            if (HideProgress) {
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetTransformMatrix());

                //_spriteBatch.DrawString(_regularFont, "Press SPACE BAR to jump/kick.", new Vector2(256f, 2048f), Color.White);
                //_spriteBatch.DrawString(_regularFont, "Use the ARROW KEYS to move.", new Vector2(60f, 1700f), Color.White);

                _spriteBatch.End();
            }

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Vector2 progPos = new Vector2(32f, _graphicsDevice.Viewport.Height / 2f);

            if (!HideProgress)
                _spriteBatch.Draw(_progressTexture, progPos + new Vector2(-_progressTexture.Width / 2f, -_progressTexture.Height / 2f), Color.White);

            Vector2 startView = GraphicsConstants.PhysicsToView(Start);
            float finishView = GraphicsConstants.PhysicsToView(level.FinishHeight);

            if (level.EntityWorld.TryGetEntity(TorchEntityID, out Entity? torchEntity)) {
                if (level.PhysicsWorld.TryGetBody(torchEntity.BodyID, out Body? torchBody)) {
                    Vector2 torchPosition = GraphicsConstants.PhysicsToView(torchBody.Position + torchBody.Bounds.Center);

                    if (!torchEntity.IsPutOut) {
                        Vector2 arrowPosition = torchPosition;

                        arrowPosition.X = MathHelper.Clamp(arrowPosition.X, _camera.Position.X + 32f, _camera.Position.X + _graphicsDevice.Viewport.Width - 32f);
                        arrowPosition.Y = MathHelper.Clamp(arrowPosition.Y, _camera.Position.Y + 32f, _camera.Position.Y + _graphicsDevice.Viewport.Height - 32f);

                        _arrowSprite.Rotation = (torchPosition - arrowPosition).GetAngle();

                        _arrowSprite.Draw(_spriteBatch, arrowPosition - _camera.Position);
                    }

                    float tiy = Math.Clamp((torchPosition.Y - finishView) / (startView.Y - finishView), 0f, 1f);

                    if (!HideProgress)
                    _spriteBatch.Draw(_torchIconTexture, 
                        progPos + new Vector2(-_torchIconTexture.Width / 2f, (tiy - 0.5f) * (_progressTexture.Height - 32f) - _torchIconTexture.Height / 2f), 
                        Color.White);
                }
            }

            if (level.EntityWorld.TryGetEntity(PlayerEntityID, out Entity? playerEntity)) {
                if (level.PhysicsWorld.TryGetBody(playerEntity.BodyID, out Body? playerBody)) {
                    Vector2 playerPosition = GraphicsConstants.PhysicsToView(playerBody.Position + playerBody.Bounds.Center);

                    float piy = Math.Clamp((playerPosition.Y - finishView) / (startView.Y - finishView), 0f, 1f);

                    if (!HideProgress)
                        _spriteBatch.Draw(_playerIconTexture,
                        progPos + new Vector2(-_playerIconTexture.Width / 2f, (piy - 0.5f) * (_progressTexture.Height - 32f) - _playerIconTexture.Height / 2f),
                        Color.White);
                }
            }

            _spriteBatch.Draw(_pixelTexture, _graphicsDevice.Viewport.Bounds, Color.Black * 0.5f * _loseScreenOpacity);

            _spriteBatch.DrawString(_regularFont, "Press R to restart.",
                _graphicsDevice.Viewport.Bounds.Center.ToVector2() - _regularFont.MeasureString("Press R to restart.") / 2f, Color.White * _loseScreenOpacity);

            _spriteBatch.Draw(_pixelTexture, _graphicsDevice.Viewport.Bounds, Color.Black * _fadeOutOpacity);

            if (Header != null) {
                float headerOpacity = MathHelper.Clamp(1f - _headerTimer / _headerTime, 0f, 1f);

                _spriteBatch.Draw(_gradientTexture, Vector2.Zero, Color.White * headerOpacity);
                _spriteBatch.DrawString(_regularFont, Header, new Vector2(_graphicsDevice.Viewport.Bounds.Center.X - _regularFont.MeasureString(Header).X / 2f, 24f), Color.White * headerOpacity);
            }

            _spriteBatch.End();
        }

        private bool RenderTargetsAreOutdated() {
            return _worldTarget.Width != _graphicsDevice.Viewport.Width + _extraSize
                || _worldTarget.Height != _graphicsDevice.Viewport.Height + _extraSize;
        }

        private void UpdateRenderTargets() {
            _worldTarget.Dispose();
            _waterTarget.Dispose();

            _worldTarget = CreateRenderTarget();
            _waterTarget = CreateRenderTarget();
        }

        private RenderTarget2D CreateRenderTarget() {
            return new RenderTarget2D(_graphicsDevice,
                _graphicsDevice.Viewport.Width + _extraSize,
                _graphicsDevice.Viewport.Height + _extraSize,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8,
                0,
                RenderTargetUsage.PreserveContents);
        }
    }
}
