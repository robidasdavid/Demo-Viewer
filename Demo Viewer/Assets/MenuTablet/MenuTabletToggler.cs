using UnityEngine;
using unityutilities;

namespace MenuTablet
{
    public class MenuTabletToggler : MonoBehaviour
    {
        public MenuTabletMover tablet;
        public KeyCode nonVRToggle;
        public Rig rig;

        private void Start()
        {
            if (rig == null)
            {
                rig = FindObjectOfType<Rig>();
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKeyDown(nonVRToggle))
            {
                tablet.hand = null;
                tablet.ToggleTabletInstance();
            }

            if (InputMan.Button2Down(Side.Left))
            {
                tablet.hand = rig.leftHand;
                tablet.ToggleTabletInstance();
            }

            if (InputMan.Button2Down(Side.Right))
            {
                tablet.hand = rig.rightHand;
                tablet.ToggleTabletInstance();
            }
        }
    }
}
