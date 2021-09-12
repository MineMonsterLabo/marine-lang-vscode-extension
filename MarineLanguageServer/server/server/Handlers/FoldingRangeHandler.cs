using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MarineLang.LanguageServerImpl.Handlers
{
    public class FoldingRangeHandler : IFoldingRangeHandler
    {
        public FoldingRangeRegistrationOptions GetRegistrationOptions(FoldingRangeCapability capability,
            ClientCapabilities clientCapabilities)
        {
            throw new NotImplementedException();
        }

        public Task<Container<FoldingRange>> Handle(FoldingRangeRequestParam request,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}