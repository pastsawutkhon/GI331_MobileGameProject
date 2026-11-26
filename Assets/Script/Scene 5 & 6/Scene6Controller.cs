using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Solution
{
    public sealed class Scene6Controller : MonoBehaviour
    {
        public const string CardName = "SD_Card";
        private const string ScrewdriverName = "Screwdriver";
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private RectTransform hud;
        [SerializeField] private Font thaiFont;
        [SerializeField] private Sprite evidenceRoom;
        [SerializeField] private Sprite stairwell;
        [SerializeField] private Shader cleanPaperShader;
        [SerializeField] private AudioClip metalDrop;
        private Material cleanPaper;
        private RectTransform cameraGuide;
        private Image cardTrail, choices;
        [SerializeField] private Sprite tunnel;
        [SerializeField] private Sprite reverseTunnel;
        [SerializeField] private AudioClip footsteps;
        [SerializeField] private AudioClip tapePeel;
        [SerializeField] private AudioClip cardInsert;
        [SerializeField] private AudioClip nearStep;
        [SerializeField] private Sprite tunnelBrother;
        [SerializeField] private AudioClip roomAmbience;
        [SerializeField] private AudioClip tunnelAmbience;
        private const float AmbienceVolume = .52f;
        private AudioSource ambience;
        private AudioSource speaker;
        private Image darkness;
        [SerializeField] private Sprite brother;
        [SerializeField] private Sprite sdIcon;
        [SerializeField] private Sprite grate;
        [SerializeField] private Sprite screwdriver;
        [SerializeField] private VideoPlayer player;

        private enum Stage { Taped, Loose, Carried, Recording, FindTool, Escape, Chase, Choice, Ending }
        private Stage stage;
        private CameraSlot cameraSlot;
        private Image picture, tape, brotherImage, exit, tool, shade;
        private Button card;
        private Text line;
        private RawImage movie;
        private RenderTexture target;
        private bool finishedVideo, failedVideo;
        public bool CanInsertCard => stage == Stage.Carried;

        private void Start()
        {
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.spatialBlend = 0;
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.spatialBlend = 0;
            ambience.volume = AmbienceVolume;
            ambience.clip = roomAmbience;
            ambience.Play();
            cameraSlot = inventoryUI.GetComponentInChildren<CameraSlot>(true);
            cameraSlot.gameObject.AddComponent<Scene6CameraSlot>().Story = this;

            picture = ImageAt("Background Room 2", hud, evidenceRoom, 0, 0, 1, 1);
            cleanPaper = new Material(cleanPaperShader);
            picture.material = cleanPaper;
            picture.transform.SetAsFirstSibling();
            var room = picture.rectTransform;
            tape = ImageAt("Tape on card",room,null,.806f,.491f,.875f,.505f);
            tape.color = new Color(.66f,.57f,.39f,.84f);
            tape.rectTransform.localRotation = Quaternion.Euler(0,0,-11);
            card = TapArea("SD card on evidence wall",room,.78f,.431f,.92f,.54f,TakeCard);

            brotherImage = ImageAt("ธีรวัฒน์",room,brother,.44f,.04f,.98f,.82f);
            brotherImage.preserveAspect = true;
            brotherImage.gameObject.SetActive(false);
            exit = ImageAt("Drain cover",room,grate,.22f,.02f,.50f,.075f);
            Click(exit,EnterSewer);
            exit.raycastTarget = false;
            exit.gameObject.SetActive(false);
            tool = ImageAt("Screwdriver on floor",room,screwdriver,.12f,.355f,.31f,.445f);
            tool.preserveAspect = true;
            Click(tool,PickUpScrewdriver);
            tool.gameObject.SetActive(false);
            darkness = ImageAt("Room lighting",room,null,0,0,1,1);
            SetDarkness(0);
            shade = ImageAt("Dialogue backing",room,null,.04f,.77f,.96f,.96f);
            shade.color = new Color(.025f,.025f,.025f,.82f);
            line = TextAt(shade.rectTransform,.04f,.05f,.96f,.95f);
            Say("");
            ChapterDialogue.Show("S05_PAT_02", "S05_PAT_03", "S05_PAT_04");
        }

        private static void Position(RectTransform rect, Transform parent,float x,float y,float right,float top)
        {
            rect.SetParent(parent,false);
            rect.anchorMin = new Vector2(x,y);
            rect.anchorMax = new Vector2(right,top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image ImageAt(string name, Transform parent, Sprite sprite,float x,float y,float right,float top)
        {
            var image = new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            Position(image.rectTransform,parent,x,y,right,top);
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private Text TextAt(Transform parent,float x,float y,float right,float top)
        {
            var text = new GameObject("Text",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            Position(text.rectTransform,parent,x,y,right,top);
            text.font = thaiFont;
            text.color = new Color(.96f,.94f,.86f);
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 46;
            text.raycastTarget = false;
            return text;
        }

        private static Button Click(Image image, UnityEngine.Events.UnityAction action)
        {
            image.raycastTarget = true;
            var btn = image.gameObject.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(action);
            return btn;
        }

        private static Button TapArea(string name,Transform parent,float x,float y,float right,float top,UnityEngine.Events.UnityAction action)
        {
            var image = ImageAt(name,parent,null,x,y,right,top);
            image.color = Color.clear;
            return Click(image,action);
        }

        private void Say(string message)
        {
            shade.gameObject.SetActive(message.Length>0);
            line.text = message;
            shade.transform.SetAsLastSibling();
        }

        private void TakeCard()
        {
            if (stage == Stage.Taped)
            {
                tape.gameObject.SetActive(false);
                speaker.PlayOneShot(tapePeel);
                stage = Stage.Loose;
                Say("เทปหลุดแล้ว");
                return;
            }
            if (stage != Stage.Loose) return;
            if (!inventoryUI.inventory.AddItem(CardName,sdIcon))
            {
                Say("ช่องเก็บของเต็ม");
                return;
            }
            stage = Stage.Carried;
            card.gameObject.SetActive(false);
            cleanPaper.SetFloat("_CardRemoved", 1f);
            ShowCameraGuide();
            ChapterDialogue.Show("S05_PAT_05");
            Say("ลาก SD card ไปที่กล้องในกรอบสีทอง");
        }

        private void ShowCameraGuide()
        {
            var cameraRect = (RectTransform)cameraSlot.transform;
            cameraGuide = new GameObject("Camera drop guide", typeof(RectTransform)).GetComponent<RectTransform>();
            Position(cameraGuide, cameraRect, 0, 0, 1, 1);
            var gold = new Color(1f, .76f, .22f, .94f);
            ImageAt("Top", cameraGuide, null, 0, .94f, 1, 1).color = gold;
            ImageAt("Bottom", cameraGuide, null, 0, 0, 1, .06f).color = gold;
            ImageAt("Left", cameraGuide, null, 0, .06f, .06f, .94f).color = gold;
            ImageAt("Right", cameraGuide, null, .94f, .06f, 1, .94f).color = gold;
            cardTrail = ImageAt("Card demonstration", hud, sdIcon, .5f, .5f, .5f, .5f);
            cardTrail.preserveAspect = true;
        }

        private void LateUpdate()
        {
            cleanPaper.SetFloat("_NightVision", cameraSlot.NightVisionOpacity);
            if (stage != Stage.Carried || !cardTrail) return;
            int slot = inventoryUI.inventory.slots.FindIndex(item => item.itemName == CardName);
            if (slot < 0) return;
            var source = inventoryUI.slotImages[slot].rectTransform;
            Vector2 start = hud.InverseTransformPoint(source.TransformPoint(source.rect.center));
            Vector2 end = hud.InverseTransformPoint(cameraGuide.TransformPoint(cameraGuide.rect.center));
            float height = hud.InverseTransformVector(cameraGuide.TransformVector(Vector3.up * cameraGuide.rect.height)).magnitude;
            float t = Mathf.Clamp01(Mathf.Repeat(Time.unscaledTime, 2.3f) / 1.65f);
            var path = Vector2.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
            path.y += Mathf.Sin(t * Mathf.PI) * height * .7f;
            cardTrail.rectTransform.localPosition = path;
            cardTrail.rectTransform.sizeDelta = Vector2.one * height * .58f;
            cardTrail.color = new Color(1, 1, 1, Mathf.Sin(t * Mathf.PI) * .88f);
        }

        public void PlayRecording()
        {
            if (!CanInsertCard) return;
            stage = Stage.Recording;
            speaker.PlayOneShot(cardInsert);
            cameraGuide.gameObject.SetActive(false);
            cardTrail.gameObject.SetActive(false);
            StartCoroutine(RecordingAndArrival());
        }

        private IEnumerator RecordingAndArrival()
        {
            // Let the inventory finish its pointer-up callback before hiding the dragged icon.
            yield return null;
            cameraSlot.DeactivateCamera();
            inventoryUI.gameObject.SetActive(false);
            picture.transform.SetAsLastSibling();
            Say("");
            movie = new GameObject("Camera playback",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
            Position(movie.rectTransform,picture.transform,0,0,1,1);
            movie.raycastTarget = true;
            target = new RenderTexture(360,640,0);
            target.Create();
            movie.texture = target;
            player.gameObject.SetActive(true);
            player.GetComponent<Image>().enabled = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.source = VideoSource.Url;
            player.url = Application.streamingAssetsPath + "/EvidenceRecording.mp4";
            player.isLooping = false;
            player.skipOnDrop = true;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.loopPointReached += VideoEnded;
            player.errorReceived += VideoFailed;
            ambience.volume = AmbienceVolume * .4f;
            player.Play();
            float until = Time.realtimeSinceStartup + 18f;
            while (!finishedVideo && !failedVideo && Time.realtimeSinceStartup < until) yield return null;
            player.Stop();
            ambience.volume = AmbienceVolume;
            movie.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            if (!finishedVideo)
            {
                Say("ในคลิป กล้องไล่ผ่านใบหน้าบนบอร์ด ก่อนหยุดที่อุโมงค์มืด");
                yield return new WaitForSecondsRealtime(2.5f);
            }
            Say("");
            yield return ChapterDialogue.Sequence("S06_PAT_01", "S06_PAT_02", "S06_PAT_03");
            speaker.PlayOneShot(footsteps);
            Say("เสียงฝีเท้าหนัก ๆ ดังลงมาตามบันได");
            yield return new WaitForSecondsRealtime(1.7f);
            Say("");
            yield return FadeLight(.68f, .18f);
            yield return FadeLight(.25f, .12f);
            yield return FadeLight(1, .21f);
            RevealStairs();
            yield return new WaitForSecondsRealtime(.34f);
            yield return FadeLight(.25f, .14f);
            yield return Shake(.85f);
            yield return ChapterDialogue.Sequence("S06_WAT_01", "S06_PAT_04", "S06_PAT_05");
            picture.transform.SetAsFirstSibling();
            inventoryUI.gameObject.SetActive(true);
            exit.gameObject.SetActive(true);
            tool.gameObject.SetActive(true);
            Say("ข้างฝาท่อมีไขควงขึ้นสนิมตกอยู่");
            stage = Stage.FindTool;
        }

        private void RevealStairs()
        {
            cleanPaper.SetFloat("_CardRemoved", 0f);
            picture.sprite = stairwell;
            Position(exit.rectTransform, picture.transform, .14f, .24f, .50f, .34f);
            brotherImage.gameObject.SetActive(true);
        }

        private void SetDarkness(float alpha)
        {
            darkness.color = new Color(0, 0, 0, alpha);
        }

        private IEnumerator FadeLight(float targetAlpha, float duration)
        {
            float start = darkness.color.a;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                SetDarkness(Mathf.Lerp(start, targetAlpha, t / duration));
                yield return null;
            }
            SetDarkness(targetAlpha);
        }

        private void VideoEnded(VideoPlayer _) => finishedVideo = true;
        private void VideoFailed(VideoPlayer _,string reason)
        {
            failedVideo = true;
            Debug.LogWarning("Camera recording: " + reason,this);
        }

        private IEnumerator Shake(float duration)
        {
            var rect = picture.rectTransform;
            for (float t=0;t<duration;t+=Time.unscaledDeltaTime)
            {
                float strength = 1-t/duration;
                rect.localScale = Vector3.one*(1+.035f*strength);
                rect.anchoredPosition = Vector2.Scale(new Vector2(Mathf.Sin(t*83),Mathf.Cos(t*67)),rect.rect.size)*(.012f*strength);
                yield return null;
            }
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void PickUpScrewdriver()
        {
            if (stage != Stage.FindTool) return;
            var bag = inventoryUI.inventory;
            if (!bag.HasItem(ScrewdriverName, 1) && !bag.AddItem(ScrewdriverName, screwdriver))
            {
                Say("ช่องเก็บของเต็ม");
                return;
            }
            tool.gameObject.SetActive(false);
            stage = Stage.Escape;
            exit.raycastTarget = true;
            Say("เก็บไขควงแล้ว แตะฝาท่อเพื่อหนี");
        }

        private void EnterSewer()
        {
            if (stage != Stage.Escape || !inventoryUI.inventory.HasItem(ScrewdriverName, 1)) return;
            stage = Stage.Chase;
            StartCoroutine(Chase());
        }

        private IEnumerator Chase()
        {
            exit.gameObject.SetActive(false);
            brotherImage.gameObject.SetActive(false);
            picture.sprite = tunnel;
            ambience.clip = tunnelAmbience;
            ambience.Play();
            Say("ธีรภัทรวิ่งฝ่าน้ำขัง เสียงฝีเท้ายังไล่ตามมา");
            yield return Shake(1.5f);
            Say("");
            yield return ChapterDialogue.Sequence("S06_PAT_06");
            Say("");
            yield return FadeLight(1, .19f);
            picture.sprite = reverseTunnel;
            yield return FadeLight(.28f, .2f);
            yield return new WaitForSecondsRealtime(.46f);
            yield return FadeLight(1, .14f);
            brotherImage.sprite = tunnelBrother;
            var figure = brotherImage.rectTransform;
            Position(figure, picture.transform, .30f, .24f, .70f, .76f);
            brotherImage.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(.35f);
            yield return FadeLight(.15f, .16f);
            speaker.PlayOneShot(footsteps, .75f);
            for (float t = 0; t < 2.35f; t += Time.unscaledDeltaTime)
            {
                float approach = t / 2.35f;
                figure.localScale = Vector3.one * Mathf.Lerp(1, 1.78f, approach);
                figure.anchoredPosition = new Vector2(Mathf.Sin(t * 5) * .005f,
                    -.065f * approach + .004f * Mathf.Cos(t * 10)) * picture.rectTransform.rect.size;
                yield return null;
            }
            yield return FadeLight(1, .12f);
            figure.localScale = Vector3.one;
            Position(figure, picture.transform, -.26f, -.64f, 1.26f, 1.02f);
            yield return new WaitForSecondsRealtime(.4f);
            speaker.PlayOneShot(nearStep);
            yield return FadeLight(.12f, .1f);
            yield return new WaitForSecondsRealtime(.65f);
            yield return ChapterDialogue.Sequence("S06_WAT_02", "S06_WAT_03", "S06_PAT_07", "S06_PAT_08");
            stage = Stage.Choice;
            choices=ImageAt("Final choice",picture.transform,null,.08f,.26f,.92f,.45f);
            choices.color=new Color(.04f,.035f,.03f,.9f);
            ChoiceButton(choices.transform,.53f,.97f,"จบชีวิตตัวเอง",false);
            ChoiceButton(choices.transform,.03f,.47f,"ใช้ไขควงสู้",true);
        }

        private void ChoiceButton(Transform parent,float bottom,float top,string label,bool fight)
        {
            var image=ImageAt(label,parent,null,.035f,bottom,.965f,top);
            image.color=new Color(.23f,.19f,.16f);
            TextAt(image.transform,.02f,.03f,.98f,.97f).text=label;
            Click(image,()=>Choose(fight));
        }

        private void Choose(bool fight)
        {
            if (stage!=Stage.Choice) return;
            stage=Stage.Ending;
            inventoryUI.gameObject.SetActive(false);
            choices.gameObject.SetActive(false);
            StartCoroutine(PlayEnding(fight));
        }

        private IEnumerator PlayEnding(bool fight)
        {
            yield return ChapterDialogue.Sequence(fight ? "END_A_PAT_01" : "END_B_PAT_01");
            Say("");
            var black=ImageAt("Cut to black",hud,null,0,0,1,1);
            black.color=Color.black;
            black.raycastTarget=true;
            if(fight) StartCoroutine(FightEnding(black));
            else ambience.Stop();
        }

        private IEnumerator FightEnding(Image black)
        {
            black.color=Color.clear;
            var figure = brotherImage.rectTransform;
            for (float t = 0; t < .34f; t += Time.unscaledDeltaTime)
            {
                float approach = t / .34f;
                figure.localScale = Vector3.one * (1 + .7f * approach * approach);
                yield return null;
            }
            speaker.PlayOneShot(metalDrop);
            yield return Shake(.16f);
            ambience.Stop();
            black.color = Color.black;
        }

        private void OnDestroy()
        {
            if (player)
            {
                player.loopPointReached -= VideoEnded;
                player.errorReceived -= VideoFailed;
                player.targetTexture=null;
            }
            if (cleanPaper) Destroy(cleanPaper);
            if(target) { target.Release(); Destroy(target); }
        }
    }
}
