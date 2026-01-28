using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251218_DockerfileFix : CodeUpgradeBase
{
    public override string Description => "Dockerfile fix";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.Server/Dockerfile", file =>
        {
            file.ReplaceBetween(
                new ReplaceBetweenOption(a => a.Contains("RUN apt-get -y install curl")),
                new ReplaceBetweenOption(a => a.Contains("COPY [\"Framework.tar\", \"/\"]"), -2),
                """
                RUN apt-get update && apt-get install -y curl && \
                    curl -SLO https://deb.nodesource.com/nsolid_setup_deb.sh && \
                    chmod 500 nsolid_setup_deb.sh && \
                    ./nsolid_setup_deb.sh 22 && \
                    apt-get install -y nodejs && \
                    npm install -g yarn && \
                    rm -rf /var/lib/apt/lists/*
                """
                );

            file.InsertAfterFirstLine(a => a.Contains($"COPY [\"{uctx.ApplicationName}/{uctx.ApplicationName}.csproj\", \"{uctx.ApplicationName}/\"]"),
                $"COPY [\"{uctx.ApplicationName}/package.json\", \"{uctx.ApplicationName}/\"]");

            file.InsertBeforeFirstLine(a => a.Contains("COPY --from=publish /app/publish ."),
                """
                RUN apt-get update && \
                    apt-get install -y libgdiplus libfontconfig1 libgssapi-krb5-2 && \
                    rm -rf /var/lib/apt/lists/*
                """
            );
        });
    }
}
