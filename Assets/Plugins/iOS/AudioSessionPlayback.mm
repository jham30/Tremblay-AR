// Pone la sesión de audio de iOS en modo "Playback" para que el sonido
// se escuche aunque el iPhone tenga activado el interruptor de silencio.
// MixWithOthers permite que siga sonando la música de otras apps.
#import <AVFoundation/AVFoundation.h>

extern "C" void _ConfigurarAudioPlayback()
{
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSError *error = nil;
    [session setCategory:AVAudioSessionCategoryPlayback
             withOptions:AVAudioSessionCategoryOptionMixWithOthers
                   error:&error];
    if (error != nil)
        NSLog(@"[AudioSessionPlayback] setCategory falló: %@", error);
    [session setActive:YES error:nil];
}
