using System;
using System.IO;
using DocumentationProcessor.Properties;
using DocumentationProcessor.Web;

namespace DocumentationProcessor;

internal sealed class Program {
    private static int Main() {
        Uri tempPath = new(Path.GetTempPath());
        if (DocsDownloader.ValidateDownloadDirectory(tempPath))
            DocsDownloader.FetchCppRefDocs(tempPath);

        return 0;
    }
};
