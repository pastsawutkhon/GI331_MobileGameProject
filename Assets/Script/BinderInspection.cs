using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Solution
{
    public class BinderInspection : MonoBehaviour
    {
        [SerializeField] private Sprite[] pages;
        [SerializeField] private Sprite closeIcon;
        public static bool IsOpen { get; private set; }
        private GameObject viewer;
        private RectTransform stack;
        private readonly List<Image> sheets = new List<Image>();
        private readonly List<int> order = new List<int>();
        private Text counter;
        private Button previous, next, exit;
        private bool turning;
        private Coroutine turn;
        public int CurrentPage => order.Count > 0 ? order[0] : -1;

        public static bool TryInspect(string item)
        {
            if (item != "Patient Binder" || ReturnableNote.IsInspecting) return false;
            var binder = FindFirstObjectByType<BinderInspection>();
            if (binder == null || binder.pages == null || binder.pages.Length != 3) return false;
            binder.Open();
            return true;
        }

        private void Open()
        {
            IsOpen = true;
            viewer = new GameObject("Patient Binder Inspection", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = viewer.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = viewer.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(810,1440);
            scaler.matchWidthOrHeight = 0.5f;
            var backdrop = ImageObject("Backdrop", viewer.transform, new Color(0,0,0,0.84f));
            var full = backdrop.rectTransform;
            full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            Rect bounds = ((RectTransform)viewer.transform).rect;
            var content = new GameObject("Document Stack", typeof(RectTransform));
            content.transform.SetParent(backdrop.transform,false);
            stack = (RectTransform)content.transform;
            stack.anchorMin = stack.anchorMax = new Vector2(0.5f,0.51f);
            float height = Mathf.Min(bounds.height*0.73f,bounds.width*0.82f/0.69f);
            stack.sizeDelta = new Vector2(height*0.69f,height);
            sheets.Clear(); order.Clear();
            for(int i=0;i<3;i++)
            {
                var page=ImageObject("Patient File "+(i+1),stack,Color.white);
                page.sprite=pages[i]; page.preserveAspect=true; page.raycastTarget=false;
                page.rectTransform.sizeDelta=stack.sizeDelta;
                var shadow=page.gameObject.AddComponent<Shadow>();
                shadow.effectColor=new Color(0,0,0,0.22f);shadow.effectDistance=new Vector2(2,-3);
                sheets.Add(page);order.Add(i);
            }
            Layout();
            previous=Arrow(backdrop.transform,false);
            next=Arrow(backdrop.transform,true);
            previous.onClick.AddListener(()=>TurnPage(false));
            next.onClick.AddListener(()=>TurnPage(true));
            counter=TextObject("Page Number",backdrop.transform,new Vector2(0.5f,0.09f),new Vector2(130,64));
            counter.fontSize=25; counter.color=new Color(0.78f,0.73f,0.63f);
            UpdateCounter();
            exit=ButtonObject("Close",backdrop.transform,new Vector2(0.90f,0.93f),new Vector2(80,80));
            var icon=ImageObject("Close Icon",exit.transform,Color.white);
            icon.sprite=closeIcon;icon.preserveAspect=true;icon.raycastTarget=false;
            icon.rectTransform.sizeDelta=new Vector2(44,44);
            exit.onClick.AddListener(Close);
            viewer.AddComponent<InspectionTransition>().Show(stack);
        }

        public void TurnPage(bool forward)
        {
            if(viewer==null||turning||!viewer.GetComponent<InspectionTransition>().IsReady)return;
            turn=StartCoroutine(Turn(forward));
        }

        private IEnumerator Turn(bool forward)
        {
            turning=true;
            previous.interactable=next.interactable=exit.interactable=false;
            Image moving=sheets[forward?0:2];
            Vector2 start=moving.rectTransform.anchoredPosition;
            Quaternion rotation=moving.rectTransform.localRotation;
            Vector2 outside=new Vector2(-stack.rect.width*1.02f,-12);
            float elapsed=0;
            while(elapsed<0.26f)
            {
                elapsed+=Time.unscaledDeltaTime;
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/0.26f));
                moving.rectTransform.anchoredPosition=Vector2.Lerp(start,outside,t);
                moving.rectTransform.localRotation=Quaternion.Slerp(rotation,Quaternion.Euler(0,0,7),t);
                yield return null;
            }
            int page=order[forward?0:2];
            sheets.Remove(moving);order.RemoveAt(forward?0:2);
            if(forward){sheets.Add(moving);order.Add(page);}else{sheets.Insert(0,moving);order.Insert(0,page);}
            for(int i=2;i>=0;i--)sheets[i].transform.SetAsLastSibling();
            Vector2[] positions=new Vector2[3];Quaternion[] rotations=new Quaternion[3];Vector3[] scales=new Vector3[3];
            for(int i=0;i<3;i++){positions[i]=sheets[i].rectTransform.anchoredPosition;rotations[i]=sheets[i].rectTransform.localRotation;scales[i]=sheets[i].rectTransform.localScale;}
            elapsed=0;
            while(elapsed<0.30f)
            {
                elapsed+=Time.unscaledDeltaTime;
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/0.30f));
                for(int i=0;i<3;i++)
                {
                    var r=sheets[i].rectTransform;
                    r.anchoredPosition=Vector2.Lerp(positions[i],DepthPosition(i),t);
                    r.localRotation=Quaternion.Slerp(rotations[i],DepthRotation(i),t);
                    r.localScale=Vector3.Lerp(scales[i],Vector3.one*(1-i*0.012f),t);
                }
                yield return null;
            }
            Layout(); UpdateCounter();
            turning=false;turn=null;
            previous.interactable=next.interactable=exit.interactable=true;
        }

        private static Vector2 DepthPosition(int depth)=>new Vector2(depth*10,depth*9);
        private static Quaternion DepthRotation(int depth)=>Quaternion.Euler(0,0,depth==0?0:depth==1?-1.1f:1.3f);
        private void Layout()
        {
            for(int i=2;i>=0;i--)
            {
                var r=sheets[i].rectTransform;
                r.SetAsLastSibling();r.anchoredPosition=DepthPosition(i);r.localRotation=DepthRotation(i);
                r.localScale=Vector3.one*(1-i*0.012f);
            }
        }
        private void UpdateCounter(){counter.text=(order[0]+1)+" / 3";}
        public void Close()
        {
            if(viewer==null||turning||!viewer.GetComponent<InspectionTransition>().IsReady)return;
            var current=viewer;
            current.GetComponent<InspectionTransition>().Dismiss(()=>{viewer=null;IsOpen=false;sheets.Clear();order.Clear();});
        }
        private void OnDisable()
        {
            if(turn!=null)StopCoroutine(turn);
            turn=null;turning=false;
            if(viewer!=null)Destroy(viewer);
            viewer=null;IsOpen=false;sheets.Clear();order.Clear();
        }
        private static Image ImageObject(string name,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.color=color;return image;
        }
        private static Button ButtonObject(string name,Transform parent,Vector2 anchor,Vector2 size)
        {
            var image=ImageObject(name,parent,Color.white);
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.clear;colors.selectedColor=Color.clear;
            colors.disabledColor=Color.clear;colors.highlightedColor=new Color(1,0.9f,0.7f,0.08f);
            colors.pressedColor=new Color(1,0.9f,0.7f,0.18f);colors.fadeDuration=0.06f;button.colors=colors;
            var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
            return button;
        }
        private static Button Arrow(Transform parent,bool right)
        {
            var button=ButtonObject(right?"Next Page":"Previous Page",parent,new Vector2(right?0.72f:0.28f,0.09f),new Vector2(100,80));
            for(int i=0;i<2;i++)
            {
                var stroke=ImageObject("Chevron",button.transform,new Color(0.87f,0.82f,0.71f));stroke.raycastTarget=false;
                stroke.rectTransform.sizeDelta=new Vector2(24,4);
                stroke.rectTransform.anchoredPosition=new Vector2(0,i==0?7:-7);
                stroke.rectTransform.localRotation=Quaternion.Euler(0,0,(i==0?-40:40)*(right?1:-1));
            }
            return button;
        }
        private static Text TextObject(string name,Transform parent,Vector2 anchor,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;
            var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
    }
}
