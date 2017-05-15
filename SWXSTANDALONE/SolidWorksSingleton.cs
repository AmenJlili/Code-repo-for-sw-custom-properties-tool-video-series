using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWXSTANDALONE
{
    internal class SolidWorksSingleton
    {
        private static SldWorks swApp;

        private SolidWorksSingleton()
        {
        }
        // get solidworks 
        internal static SldWorks getApplication()
        {
            if (swApp == null)
            {
                swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
                swApp.Visible = true;

                return swApp;
            }

            return swApp;
        }

        // get solidworks async
        internal async static Task<SldWorks> getApplicationAsync()
        {
            if (swApp == null)
            {
                return await Task<SldWorks>.Run(() => {

                    swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
                  

                    return swApp;

                });
            }

            return swApp;
        }

        // dipose of solidworks
        internal static void Dipose()
        {
            if (swApp != null)
            {
                swApp.ExitApp();
                swApp = null;
            }

        }

    }
}
