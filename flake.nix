{
  description = "An open source force-feedback racing simulator wheel driver.";

  inputs = {
    # Nixpkgs / NixOS version to use.
    nixpkgs.url = "nixpkgs/nixos-23.05";
  };

  outputs = inputs @ { self, nixpkgs }: let
    inherit (nixpkgs.lib) genAttrs;

    # System types to support.
    supportedSystems = [ "x86_64-linux" "x86_64-darwin" ];

    # Helper function to generate an attrset '{ x86_64-linux = f "x86_64-linux"; ... }'.
    forAllSystems = f: genAttrs supportedSystems (system: f system);

  in rec {
    # Nixpkgs instantiated for supported system types.
    nixpkgsFor = forAllSystems (system: import nixpkgs { inherit system; overlays = [ self.overlay ]; });

    # A Nixpkgs overlay.
    overlay = final: prev: {
      opentabletdriver = with final; final.callPackage ./default.nix {};
    };

    # Provide binary packages for selected system types.
    packages = forAllSystems (system: {
      inherit (nixpkgsFor.${system}) opentabletdriver;
    });

    # The default package for 'nix build'. This makes sense if the
    # flake provides only one package or there is a clear "main"
    # package.
    defaultPackage = forAllSystems (system: self.packages.${system}.opentabletdriver);

    # Provide a 'nix develop' environment for interactive hacking.
    devShell = forAllSystems (system:
      import ./shell.nix {
        inherit system;
        flake = self;
      }
    );
  };
}
