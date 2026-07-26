using UnityEngine;

namespace MeixiEcho
{
    /// <summary>
    /// 第一章村民交互：保持脚底稳定，只在玩家靠近时自然转身并播放轻微呼吸感。
    /// </summary>
    public sealed class VillageNpc3D : MonoBehaviour
    {
        [SerializeField] private MeixiGame3D game;
        [SerializeField] private Transform player;
        [SerializeField] private string[] firstDialogue;
        [SerializeField] private string repeatDialogue;
        private bool hasTalked;
        private float idlePhase;
        private Transform visualRoot;
        private Vector3 visualBasePosition;

        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string Role { get; private set; }
        [field: SerializeField] public string Glyph { get; private set; }

        public void Configure(MeixiGame3D owner, Transform playerTransform, string displayName, string role,
            string[] dialogue, string repeatLine, string glyph)
        {
            game = owner;
            player = playerTransform;
            DisplayName = displayName;
            Role = role;
            firstDialogue = dialogue;
            repeatDialogue = repeatLine;
            Glyph = glyph;
            hasTalked = false;
            idlePhase = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.19f);
            if (transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
                visualBasePosition = visualRoot.localPosition;
            }
        }

        public void Rebind(MeixiGame3D owner, Transform playerTransform)
        {
            game = owner;
            player = playerTransform;
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = gameObject.name.Replace("_可交互NPC", string.Empty).Replace("NPC", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(DisplayName)) DisplayName = "梅溪村民";
            if (string.IsNullOrWhiteSpace(Role)) Role = "梅溪村民";
            if (string.IsNullOrWhiteSpace(Glyph)) Glyph = "人";
            idlePhase = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.19f);
            if (transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
                visualBasePosition = visualRoot.localPosition;
            }
        }

        private void Update()
        {
            if (player == null) return;
            Vector3 delta = player.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 5.2f && delta.sqrMagnitude > 0.02f)
            {
                Quaternion target = Quaternion.LookRotation(delta.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 2.8f);
            }

            if (visualRoot != null)
            {
                float breath = Mathf.Sin(Time.time * 1.35f + idlePhase) * 0.0035f;
                visualRoot.localPosition = visualBasePosition + Vector3.up * breath;
            }
        }

        public void Interact()
        {
            if (game == null) return;
            string[] lines = hasTalked && !string.IsNullOrWhiteSpace(repeatDialogue)
                ? new[] { repeatDialogue }
                : firstDialogue;
            if (lines == null || lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
                lines = new[] { $"{DisplayName}｜梅溪的声音淡了很久。沿着青石路往溪边走，那里还留着第一段魂音。" };
            hasTalked = true;
            game.BeginNpcDialogue(this, lines);
        }

        public void FacePlayerImmediately()
        {
            if (player == null) return;
            Vector3 delta = player.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.02f) transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }
    }
}
