using IrisClassifier.Cntk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrisClassifier.WebApi
{
    public class ClassifyRequest
    {
        public ClassifyRequest()
        {
            Input = new ModelInput();
        }

        public string Token { get; set; }
        public ModelInput Input { get; set; }
    }
    public class ClassifyResponse
    {
        public ClassifyResponse()
        {
            Output = new ModelOutput();
        }
        public ModelOutput Output { get; set; }
    }
}
