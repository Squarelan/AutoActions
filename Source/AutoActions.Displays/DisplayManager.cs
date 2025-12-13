using System;
using System.Linq;

namespace AutoActions.Displays
{
    public class DisplayManagerHandler
    {
        public static GraphicsCardType GraphicsCardType => Instance.GraphicsCardType;


        private static IDisplayManagerBase _instance = null;

        public static IDisplayManagerBase Instance
        {
            get
            {
                if (_instance == null)
                {
                    try
                    {
                        NvAPIWrapper.NVIDIA.Initialize();
                        if (NvAPIWrapper.GPU.PhysicalGPU.GetPhysicalGPUs().Count() > 0)
                        {
                            _instance = new DisplayManagerNvidia();
                        }
                        else
                        {
                            _instance = new DisplayManagerGeneric();
                        }
                    }
                    catch (Exception)
                    {
                        _instance = new DisplayManagerGeneric();
                    }
                }
                return _instance;
            }
        }

    }
}
