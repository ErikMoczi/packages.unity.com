using System;
using Bee.Core;
using Bee.Stevedore;

class BuildProgram
{


    static StevedoreArtifact TinyDist => StevedoreArtifact.Testing("tiny-dist/24b467d6656b00060f60cba04aa94438e2416ee1_fb80481159e1b247fa9990f42cd7b84b69c130101fd15f45e967354ad533d8b0.7z");

    static StevedoreArtifact TinySamples => StevedoreArtifact.Testing("tiny-samples/24b467d6656b00060f60cba04aa94438e2416ee1_0aa6216384de2bba66969d0e60403ed2f7973b947832c2b4b29438df1a671f50.7z");

    static void Main()
    {
        Backend.Current.Register(TinyDist);
        Backend.Current.Register(TinySamples);
    }
}

