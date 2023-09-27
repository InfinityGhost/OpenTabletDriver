{ flake ? builtins.getFlake "path:${toString ./.}"
, system ? "x86_64-linux"
}:

let
  inherit (pkgs.lib) escapeShellArg makeLibraryPath;
  inherit (flake.packages.${system}) opentabletdriver;

  pkgs = flake.nixpkgsFor.${system};

in pkgs.mkShell {
  DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = 1;
  OTD_TRAY_ICON = 1;

  inputsFrom = [ opentabletdriver ];

  buildInputs = with pkgs; [
    # shellHook deps
    git
    # dev
    gnused
    gnugrep
    nixFlakes
    jq
  ];

  hardeningDisable = [ "all" ];

  shellHook = ''
    export LD_LIBRARY_PATH="${escapeShellArg (makeLibraryPath opentabletdriver.buildInputs)}"
    export REPO_ROOT="$(git rev-parse --show-toplevel)"

    export OTD_CONFIGURATIONS="$REPO_ROOT/OpenTabletDriver.Configurations/Configurations"
    export OTD_APPDATA="$REPO_ROOT/.data"
    mkdir -p "$OTD_APPDATA"
  '';
}
