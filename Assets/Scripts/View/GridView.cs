using System.Collections.Generic;
using TwoWorlds.Logic;
using UnityEngine;

namespace TwoWorlds.View
{
    /// <summary>
    /// Builds both grids from a Level (world A above world B) and keeps the sprites in sync with the game state.
    /// One cell = one Unity unit. Every visual is an instance of the same white square, tinted by code.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        private const float CellSize = 0.94f;

        [SerializeField] private SpriteRenderer cellPrefab;
        [SerializeField] private float worldGap = 1.5f;
        [SerializeField] private float screenMargin = 1.5f;

        [Header("Colors")] [SerializeField] private Color background = new Color32(0x12, 0x14, 0x18, 0xFF);
        [SerializeField] private Color floor = new Color32(0x4A, 0x50, 0x5E, 0xFF);
        [SerializeField] private Color wall = new Color32(0x23, 0x26, 0x2E, 0xFF);
        [SerializeField] private Color spikes = new Color32(0xE5, 0x48, 0x4D, 0xFF);
        [SerializeField] private Color goal = new Color32(0x4C, 0xC2, 0x5A, 0xFF);
        [SerializeField] private Color playerA = new Color32(0x6F, 0xE3, 0xFF, 0xFF);
        [SerializeField] private Color playerB = new Color32(0xFF, 0xD1, 0x66, 0xFF);
        [SerializeField] private Color mirrorPlayer = new Color32(0xB0, 0x72, 0x66, 0xFF);
        [SerializeField] private Color turret = new Color32(0xE5, 0x48, 0x2A, 0xFF);
        [SerializeField] private Color trigger = new Color32(0x5, 0x48, 0x2A, 0xFF);
        [SerializeField] private Color projectile = new Color32(0xFF, 0xF1, 0xA8, 0xFF);
        [SerializeField] private Color death = new Color32(0xFF, 0x20, 0x20, 0xFF);

        [Header("Motion")]
        [Tooltip("Cells per second when a character slides to its new cell.")]
        [SerializeField] private float playerSpeed = 12f;

        [SerializeField] private Color[] letterColors =
        {
            new Color32(0xFF, 0x8C, 0x42, 0xFF), // A
            new Color32(0x3D, 0x7D, 0xFF, 0xFF), // B
            new Color32(0xB0, 0x72, 0xFF, 0xFF), // C
            new Color32(0x2E, 0xC4, 0xB6, 0xFF), // D
        };

        /// <summary>A sprite that slides toward its target cell instead of teleporting.</summary>
        private sealed class Glider
        {
            public SpriteRenderer Sprite;
            public Vector3 Target;
        }

        private readonly Vector2[] origins = new Vector2[Level.PlayerCount];
        private readonly Glider[] players = new Glider[Level.PlayerCount];
        private readonly Color[] playerColors = new Color[Level.PlayerCount];
        private readonly Dictionary<char, List<SpriteRenderer>> doors = new Dictionary<char, List<SpriteRenderer>>();
        private readonly Dictionary<int, Glider> projectiles = new Dictionary<int, Glider>(); // key = Projectile.Id
        private float projectileSpeed;
        private Rect bounds;
        private Vector2Int lastScreenSize;

        /// <param name="tickInterval">Seconds between two projectile moves, so a projectile slides one cell per tick.</param>
        public void Build(Level level, float tickInterval)
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            doors.Clear();
            projectiles.Clear();
            projectileSpeed = 1f / tickInterval;

            World a = level.Worlds[0], b = level.Worlds[1];
            int width = Mathf.Max(a.Width, b.Width);
            origins[1] = new Vector2((width - b.Width) / 2f, 0f);
            origins[0] = new Vector2((width - a.Width) / 2f, b.Height + worldGap);

            for (int i = 0; i < Level.PlayerCount; i++)
            {
                var parent = new GameObject(i == 0 ? "World A" : "World B").transform;
                parent.SetParent(transform, false);
                BuildWorld(level.Worlds[i], origins[i], parent);

                World world = level.Worlds[i];
                Color playerColor = world.IsMirror ? mirrorPlayer : (i == 0 ? playerA : playerB);
                string playerName = world.IsMirror ? "Mirror Player" : "Player";
                var start = CellToWorld(i, world.Start);
                players[i] = new Glider { Sprite = Spawn(parent, start, playerColor, 0.7f, 10, playerName), Target = start };
                playerColors[i] = playerColor;
            }

            bounds = new Rect(-0.5f, -0.5f, width, a.Height + worldGap + b.Height);
            FrameCamera();
        }

        /// <param name="snap">Teleport instead of sliding (level start, restart, after a death).</param>
        public void Refresh(GameLogic game, bool snap = false)
        {
            for (int i = 0; i < Level.PlayerCount; i++)
            {
                players[i].Target = CellToWorld(i, game.State.Players[i]);
                players[i].Sprite.color = playerColors[i]; // clears the death flash
                if (snap) players[i].Sprite.transform.position = players[i].Target;
            }

            foreach (var pair in doors)
            {
                float alpha = game.IsDoorOpen(pair.Key) ? 0.2f : 1f;
                foreach (var door in pair.Value)
                {
                    var c = door.color;
                    door.color = new Color(c.r, c.g, c.b, alpha);
                }
            }

            SyncProjectiles(game.State.Projectiles);
        }

        /// <summary>Shows a character dying on a cell. The next Refresh puts everything back.</summary>
        public void ShowDeath(int player, Pos cell)
        {
            players[player].Target = CellToWorld(player, cell);
            players[player].Sprite.color = death;
        }

        public Vector3 CellToWorld(int world, Pos p) => origins[world] + new Vector2(p.X, p.Y);

        // Creates a sprite for each new projectile and destroys the sprites of projectiles that are gone.
        private void SyncProjectiles(List<Projectile> current)
        {
            var alive = new HashSet<int>();
            foreach (var p in current)
            {
                alive.Add(p.Id);
                var target = CellToWorld(p.World, p.Pos);
                if (!projectiles.TryGetValue(p.Id, out var glider))
                {
                    var sprite = Spawn(transform, target, projectile, 0.35f, 5, "Projectile");
                    sprite.transform.rotation = Quaternion.Euler(0, 0, 45);
                    projectiles[p.Id] = glider = new Glider { Sprite = sprite };
                }
                glider.Target = target;
            }

            var gone = new List<int>();
            foreach (var id in projectiles.Keys)
                if (!alive.Contains(id)) gone.Add(id);
            foreach (var id in gone)
            {
                Destroy(projectiles[id].Sprite.gameObject);
                projectiles.Remove(id);
            }
        }

        private void Update()
        {
            foreach (var player in players)
                if (player != null) Glide(player, playerSpeed);
            foreach (var p in projectiles.Values) Glide(p, projectileSpeed);
        }

        private static void Glide(Glider glider, float speed)
        {
            var t = glider.Sprite.transform;
            t.position = Vector3.MoveTowards(t.position, glider.Target, speed * Time.deltaTime);
        }

        private void BuildWorld(World world, Vector2 origin, Transform parent)
        {
            for (int x = 0; x < world.Width; x++)
            for (int y = 0; y < world.Height; y++)
            {
                var tile = world[new Pos(x, y)];
                var pos = origin + new Vector2(x, y);
                Spawn(parent, pos, tile.Kind == TileKind.Wall ? wall : floor, CellSize, 0, $"Cell {x},{y}");

                switch (tile.Kind)
                {
                    case TileKind.Spikes:
                        Spawn(parent, pos, spikes, 0.55f, 1, "Spikes").transform.rotation = Quaternion.Euler(0, 0, 45);
                        break;
                    case TileKind.Goal:
                        Spawn(parent, pos, goal, 0.8f, 1, "Goal");
                        break;
                    case TileKind.Button:
                        Spawn(parent, pos, LetterColor(tile.Letter), 0.4f, 1, $"Button {tile.Letter}");
                        break;
                    case TileKind.Door:
                        if (!doors.TryGetValue(tile.Letter, out var list))
                            doors[tile.Letter] = list = new List<SpriteRenderer>();
                        list.Add(Spawn(parent, pos, LetterColor(tile.Letter), CellSize, 1, $"Door {tile.Letter}"));
                        break;
                    case TileKind.Turret:
                        Spawn(parent, pos, turret, 0.6f, 1, "Turret");
                        // Barrel: a small square pushed toward the firing direction.
                        var offset = new Pos(0, 0).Step(tile.TurretDirection);
                        var barrelPos = pos + new Vector2(offset.X, offset.Y) * 0.4f;
                        Spawn(parent, barrelPos, turret, 0.3f, 1, "Barrel");
                        break;
                    case TileKind.Trigger:
                        Spawn(parent, pos, trigger, 0.5f, 1, "Trigger").transform.rotation = Quaternion.Euler(0, 0, 45);
                        break;
                }
            }
        }

        private SpriteRenderer Spawn(Transform parent, Vector3 pos, Color color, float size, int order,
            string objectName)
        {
            var sr = Instantiate(cellPrefab, pos, Quaternion.identity, parent);
            sr.name = objectName;
            sr.color = color;
            sr.sortingOrder = order;
            sr.transform.localScale = Vector3.one * (size / cellPrefab.sprite.bounds.size.x);
            return sr;
        }

        private Color LetterColor(char letter) => letterColors[(letter - 'A') % letterColors.Length];

        // Re-frame when the window (or the WebGL canvas) is resized.
        private void LateUpdate()
        {
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y) FrameCamera();
        }

        private void FrameCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            float halfHeight = bounds.height / 2f + screenMargin;
            float halfWidth = bounds.width / 2f + screenMargin;
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
