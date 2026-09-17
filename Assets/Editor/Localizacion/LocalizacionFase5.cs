using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Fase 5: vuelca el texto y el audio de cada StoryFragment a las tablas Story / StoryAudio
/// (clave story.<fragmentID>) y enlaza el LocalizedString / LocalizedAudioClip del asset.
/// El texto y el audio actuales están en inglés → columna en. Idempotente.
/// </summary>
public static class LocalizacionFase5
{
    const string TablaStory = "Story";
    const string TablaStoryAudio = "StoryAudio";
    const string LocaleOrigen = "en";

    [MenuItem("Tremblay/Localización/Fase 5 - Migrar fragmentos de historia a las tablas (en)")]
    public static void MigrarFragmentos()
    {
        var textos = LocalizationEditorSettings.GetStringTableCollection(TablaStory);
        var audios = LocalizationEditorSettings.GetAssetTableCollection(TablaStoryAudio);
        var locale = LocalizationEditorSettings.GetLocale(LocaleOrigen);
        var tablaTexto = textos?.GetTable(LocaleOrigen) as StringTable;
        if (tablaTexto == null || audios == null || locale == null)
        {
            Debug.LogError("[Fase 5] Faltan las tablas Story/StoryAudio o el locale en. Ejecuta antes la Fase 0.");
            return;
        }

        var sb = new StringBuilder();
        int migrados = 0, saltados = 0, sinId = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:StoryFragment"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var f = AssetDatabase.LoadAssetAtPath<StoryFragment>(ruta);
            if (f == null) continue;

            if (string.IsNullOrWhiteSpace(f.fragmentID))
            {
                sinId++;
                sb.AppendLine($"   SIN fragmentID: {ruta}");
                continue;
            }

            if (f.texto != null && !f.texto.IsEmpty)
            {
                saltados++;
                continue;
            }

            string clave = $"story.{f.fragmentID}";
            Undo.RecordObject(f, "Migrar fragmento a tablas");

            // Texto → Story[en]
            if (!string.IsNullOrEmpty(f.textoFragmento) && !EsPlaceholder(f.textoFragmento))
            {
                var entrada = tablaTexto.GetEntry(clave);
                if (entrada == null) tablaTexto.AddEntry(clave, f.textoFragmento);
                else if (string.IsNullOrEmpty(entrada.Value)) entrada.Value = f.textoFragmento;
            }
            else if (textos.SharedData.GetEntry(clave) == null)
            {
                textos.SharedData.AddKey(clave);
            }

            f.texto = new LocalizedString();
            f.texto.SetReference(textos.SharedData.TableCollectionNameGuid, textos.SharedData.GetEntry(clave).Id);

            // Audio → StoryAudio[en]
            if (f.audioNarracion != null)
                audios.AddAssetToTable(locale.Identifier, clave, f.audioNarracion);
            else if (audios.SharedData.GetEntry(clave) == null)
                audios.SharedData.AddKey(clave);

            f.audio = new LocalizedAudioClip();
            f.audio.SetReference(audios.SharedData.TableCollectionNameGuid, audios.SharedData.GetEntry(clave).Id);

            EditorUtility.SetDirty(f);
            migrados++;
            sb.AppendLine($"   {clave}  texto={(string.IsNullOrEmpty(f.textoFragmento) ? "-" : "ok")}  audio={(f.audioNarracion ? f.audioNarracion.name : "-")}");
        }

        EditorUtility.SetDirty(tablaTexto);
        EditorUtility.SetDirty(textos.SharedData);
        EditorUtility.SetDirty(audios.SharedData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Fase 5] {migrados} fragmentos migrados, {saltados} ya migrados, {sinId} sin fragmentID.\n{sb}");
    }

    // ============================================================
    // Traducciones es/fr. Se conservan: etiquetas <color>, [Hablante] (se muestran sin corchetes)
    // y los marcadores {n} del typewriter en posiciones equivalentes.
    // ============================================================

    const string W = "<color=#BEFF7A>";
    const string Wc = "</color>";

    static readonly (string id, string en, string es, string fr)[] Traducciones =
    {
        ("00-intro",
            null,
            "Era la noche de Halloween. Un grupo de niños jugaba por el barrio, pidiendo dulces y contando historias de miedo. Entre retos y risas, uno de ellos desafió a una niña a encender una vela en la entrada de la vieja casa abandonada a las afueras del pueblo. La niña, decidida a demostrar su valor, tomó la vela y caminó hacia el porche. Pero al pisar las viejas tablas del suelo, estas cedieron bajo sus pies y cayó al oscuro sótano de la casa.",
            "C'était la nuit d'Halloween. Un groupe d'enfants jouait dans le quartier, réclamant des bonbons et racontant des histoires qui font peur. Entre défis et rires, l'un d'eux mit une fille au défi d'allumer une bougie à l'entrée de la vieille maison abandonnée au bout du village. La fille, décidée à prouver son courage, prit la bougie et marcha vers le porche. Mais quand elle posa le pied sur les vieilles planches, celles-ci cédèrent sous ses pas et elle tomba dans la cave sombre de la maison."),

        ("00",
            null,
            "Ay... ¿dónde estoy?\nMejor enciendo la linterna...\nVale, vale... calma...\n¿Qué son esos retratos?... y esas caras... parece que me miran...\nMmm... qué sombrero tan raro. Una escoba, velas, faroles...\nEste lugar da miedo.\nVoy a llamar a los chicos...\nNo... no puede ser... el teléfono no tiene batería.",
            "Aïe... où suis-je ?\nJe ferais mieux d'allumer la lampe de poche...\nBon, bon... du calme...\nC'est quoi, ces portraits ?... et ces visages... on dirait qu'ils me regardent...\nHmm... quel drôle de chapeau. Un balai, des bougies, des lanternes...\nCet endroit fait peur.\nJe vais appeler les autres...\nNon... c'est pas vrai... mon téléphone n'a plus de batterie."),

        ("01",
            null,
            "[Niña]\nNo veo nada... necesito luz...\nAquí hay una vela... Vamos, enciéndete ya...\nEso es, ahora el farol...\nBien... al menos veo un poco.",
            "[Enfant]\nJe ne vois rien... il me faut de la lumière...\nIl y a une bougie ici... Allez, allume-toi...\nVoilà, maintenant la lanterne...\nBien... au moins je vois un peu."),

        ("02",
            null,
            "[Niña]\nTengo que salir de aquí.\n¿Dónde están las escaleras?... Creo que por allí...\n¿Eso fue... un paso?...\nSí... hay alguien arriba.\n\n" + W + "\n[Bruja]\n¡Michifús! ¡Michifús, ven aquí!" + Wc,
            "[Enfant]\nJe dois sortir d'ici.\nOù est l'escalier ?... Je crois que c'est par là...\nC'était... un pas ?...\nOui... il y a quelqu'un en haut.\n\n" + W + "\n[Sorcière]\nMittens ! Mittens, viens ici !" + Wc),

        ("03",
            null,
            "[Niña]\nNo... no, la casa no está abandonada...{7}\nLas historias son ciertas... aquí vive alguien...\nEs... es la casa de una bruja.\n" + W + "\n[Bruja]\nMichifús, voy a bajar al sótano por mi escoba y mi sombrero.\n¡Esta noche tengo que completar el ritual!" + Wc,
            "[Enfant]\nNon... non, la maison n'est pas abandonnée...{7}\nLes histoires sont vraies... quelqu'un vit ici...\nC'est... c'est la maison d'une sorcière.\n" + W + "\n[Sorcière]\nMittens, je descends à la cave chercher mon balai et mon chapeau.\nCette nuit, je dois accomplir le rituel !" + Wc),

        ("04",
            null,
            "[Niña]\nPiensa... piensa... necesita su sombrero y su escoba...{3}\nAhí están... el sombrero... y la escoba...\nLos pondré aquí... en la mesita... sí... justo aquí.\nSi los encuentra... quizá se vaya...{2}\nVamos, bruja... toma tus cosas y vete... por favor...",
            "[Enfant]\nRéfléchis... réfléchis... il lui faut son chapeau et son balai...{3}\nLes voilà... le chapeau... et le balai...\nJe vais les poser ici... sur la petite table... oui... juste là.\nSi elle les trouve... elle partira peut-être...{2}\nAllez, sorcière... prends tes affaires et va-t'en... s'il te plaît..."),

        ("05",
            null,
            "[Niña]\nNo... no... está bajando...\n¡Michifús...! ¡No, no, no! ¡Silencio!\nEl gato me vio... me va a delatar...{2}\nToma... un trozo de carne...{3}\nCome, gatito... come... pero calladito, ¿vale?\n\n" + W + "\n[Bruja]\nMichifús, deja de hacer el tonto, no tengo mucho tiempo. ¡¡Vamos!!" + Wc,
            "[Enfant]\nNon... non... elle descend...\nMittens... ! Non, non, non ! Silence !\nLe chat m'a vue... il va me trahir...{2}\nTiens... un morceau de viande...{3}\nMange, minou... mange... mais en silence, d'accord ?\n\n" + W + "\n[Sorcière]\nMittens, arrête de faire l'idiot, je n'ai pas beaucoup de temps. Allez !!" + Wc),

        ("06",
            null,
            "[Niña]\nCreo... que se ha ido.\nSí... ahora hay silencio...\nPor fin puedo salir de aquí.{2}\nCada escalón cruje... pero da igual, tengo que ver qué hay arriba.",
            "[Enfant]\nJe crois... qu'elle est partie.\nOui... c'est calme maintenant...\nJe peux enfin sortir d'ici.{2}\nChaque marche grince... mais peu importe, je dois voir ce qu'il y a en haut."),

        ("07",
            null,
            "[Niña]\n{1}Uf...{1} todo está lleno de telarañas.\nIntento avanzar, pero se me pegan a la cara...{4}\nVale... con esta escoba puedo apartarlas.\nAsí...{1} paso a paso... por fin veo el interior de la casa.",
            "[Enfant]\n{1}Ouah...{1} tout est couvert de toiles d'araignée.\nJ'essaie d'avancer, mais elles se collent à mon visage...{4}\nBon... avec ce balai je peux les écarter.\nVoilà...{1} pas à pas... je vois enfin l'intérieur de la maison."),

        ("08",
            null,
            "[Niña]\nParece la cocina.\nMmm... qué raro... huele a tierra... y a humo.\n¿Qué son estos hongos?... ¿setas?... qué extraño... parecen recién cortados.\nY esas calabazas... todas tienen caras sonrientes...\nMmm... la bruja está preparando algo con esto...\nMejor no toco nada... por ahora.",
            "[Enfant]\nOn dirait la cuisine.\nHmm... c'est bizarre... ça sent la terre... et la fumée.\nC'est quoi, ces champignons ?... des amanites ?... étrange... on dirait qu'ils viennent d'être coupés.\nEt ces citrouilles... elles ont toutes un visage souriant...\nHmm... la sorcière prépare quelque chose avec tout ça...\nJe ferais mieux de ne rien toucher... pour l'instant."),

        ("09",
            null,
            "[Niña]\nAhí está... la puerta principal.\nPor fin... puedo salir.\n¡Vamos, ábrete!\n¿Qué...?\nNo... no puede ser...\n¿Una mano? Por favor, que sea de juguete... vieja... arrugada... sujeta una cadena...\n¡Se mueve!\n¡Suelta... suelta!\nNada... no puedo... aprieta demasiado...\n\n" + W + "\n[Bruja]\n¡Alguien ha encendido la luz! ¡Hay intrusos en mi casa!" + Wc,
            "[Enfant]\nLa voilà... la porte principale.\nEnfin... je peux sortir.\nAllez, ouvre-toi !\nQuoi... ?\nNon... c'est pas possible...\nUne main ? Pitié, que ce soit un jouet... vieille... ridée... elle tient une chaîne...\nElle bouge !\nLâche... lâche !\nRien... je n'y arrive pas... elle serre trop fort...\n\n" + W + "\n[Sorcière]\nQuelqu'un a allumé la lumière ! Il y a des intrus dans ma maison !" + Wc),

        ("10",
            null,
            "[Niña]\nNo... no puedo quedarme aquí...\n¡Tengo que salir!\nEl patio... sí... es el patio trasero.",
            "[Enfant]\nNon... je ne peux pas rester ici...\nJe dois sortir !\nLa cour... oui... c'est le jardin de derrière."),

        ("11",
            null,
            "[Niña]\n¡Vamos, vamos! Tengo que salir de aquí...\n¡Ahhh!... telarañas... están por todas partes...\n\n" + W + "\n[Bruja]\n¡Arañas...! ¡Hay arañas por todas partes!\n¡Aléjenlas de mí! ¡Arañas, nooo!" + Wc + "\n\n[Niña]\nLa bruja... ¿les tiene miedo?\nVaya... eso no me lo esperaba...\nQuizá... me sirva más adelante.\nBien... ¡hora de salir antes de que vuelva!",
            "[Enfant]\nAllez, allez ! Je dois sortir d'ici...\nAhhh !... des toiles d'araignée... il y en a partout...\n\n" + W + "\n[Sorcière]\nDes araignées... ! Il y a des araignées partout !\nÉloignez-les de moi ! Des araignées, nooon !" + Wc + "\n\n[Enfant]\nLa sorcière... elle en a peur ?\nEh bien... ça, je ne m'y attendais pas...\nÇa pourrait... me servir plus tard.\nBon... il est temps de sortir avant qu'elle revienne !"),

        ("13",
            null,
            "[Niña]\nAhí... sobre la mesa... el cráneo con una vela encendida...\nPerfecto... esto me ayudará a ver.\n[Asustada] ¿Un... cementerio?\nGenial... directo al cementerio...\nNo hay otra salida...\nVamos... solo corre...\n¡Un mausoleo!... Puedo esconderme ahí... rápido...",
            "[Enfant]\nLà... sur la table... le crâne avec une bougie allumée...\nParfait... ça va m'aider à voir.\n[Effrayée] Un... cimetière ?\nGénial... droit dans le cimetière...\nIl n'y a pas d'autre sortie...\nAllez... cours, c'est tout...\nUn mausolée !... Je peux me cacher là-dedans... vite..."),

        ("14",
            null,
            "[Niña]\nUna tumba abierta... espero que no haya nada dentro. Vale... tranquila... solo mira...\nEstá... vacía.\n¡Ahhh! ¡Murciélagos!\n¡Fuera, fuera!\nAquí hay algo escrito...",
            "[Enfant]\nUne tombe ouverte... j'espère qu'il n'y a rien dedans. Bon... doucement... regarde, c'est tout...\nElle est... vide.\nAhhh ! Des chauves-souris !\nDehors, dehors !\nIl y a quelque chose d'écrit ici..."),

        ("15",
            null,
            "[Niña]\n“Para que la bruja descanse en paz, su cuerpo debe estar completo en su ataúd... y ofrecerle té de hongos.”\n¿Su cuerpo... completo...?\nEntonces...\nLa mano... la mano es suya...\nClaro... la mano que sujeta la cadena.\nSí... ahora sé qué hacer.",
            "[Enfant]\n« Pour que la sorcière repose en paix, son corps doit être complet dans son cercueil... et il faut lui offrir du thé aux champignons. »\nSon corps... complet... ?\nAlors...\nLa main... la main est à elle...\nBien sûr... la main qui tient la chaîne.\nOui... maintenant je sais quoi faire."),

        ("16",
            null,
            "[Niña]\nSi la mano es de la bruja...\nEntonces... también debe tenerle miedo a las arañas.\nSí... claro... es parte de ella.\nSi consigo acercarle unas cuantas... quizá se asuste y suelte la cadena.\nSolo necesito algo para recogerlas sin tocarlas.\nLa escoba... y el sombrero.\nPerfecto... eso debería funcionar.\nVale, vamos... con cuidado...",
            "[Enfant]\nSi la main appartient à la sorcière...\nAlors... elle doit aussi avoir peur des araignées.\nOui... bien sûr... elle fait partie d'elle.\nSi j'arrive à en approcher quelques-unes... elle aura peut-être peur et lâchera la chaîne.\nIl me faut juste quelque chose pour les ramasser sans les toucher.\nLe balai... et le chapeau.\nParfait... ça devrait marcher.\nBon, allons-y... doucement..."),

        ("17",
            null,
            "[Niña]\nAhí está... la mano... todavía sujeta la cadena.\nBien... ahora.\n¡Se soltó!\nFuncionó... sí... ¡funcionó!\nNo... ¡otra vez ese gato no!\nShhh... por favor... calladito...\n\n" + W + "\n[Bruja]\n¿La encontraste, Michifús? ¡Jajaja! ¡Te atraparé!" + Wc,
            "[Enfant]\nLa voilà... la main... elle tient toujours la chaîne.\nBon... maintenant.\nElle a lâché !\nÇa a marché... oui... ça a marché !\nNon... pas encore ce chat !\nChut... s'il te plaît... pas un bruit...\n\n" + W + "\n[Sorcière]\nTu l'as trouvée, Mittens ? Hahaha ! Je vais t'attraper !" + Wc),

        ("18",
            null,
            "[Niña]\nEse gato no me deja salir.\nVale... piensa...\nYa sé lo que tengo que hacer.\nTengo que devolverle la mano a la bruja... y darle el té de hongos.\nEso la hará dormir...\nPero... ¿dónde está su tumba?\nDebe de haber más pistas... dentro del mausoleo.",
            "[Enfant]\nCe chat ne me laisse pas partir.\nBon... réfléchis...\nJe sais ce que je dois faire.\nJe dois rendre sa main à la sorcière... et lui donner le thé aux champignons.\nÇa la fera dormir...\nMais... où est sa tombe ?\nIl doit y avoir d'autres indices... dans le mausolée."),

        ("19",
            null,
            "[Niña]\nCon el susto... no había visto este libro.\n“Mausoleo... árbol... guardián del descanso...”\n¿Un árbol que guarda la tumba? Mmm...\n“Solo la luz de una calabaza encendida revela el lugar del descanso.”\nCalabazas... como las de la cocina... con caras y velas dentro.\nEntonces... si las enciendo, podré ver dónde está enterrada la bruja.\n“Su luz mantiene lejos a los espíritus del árbol.”\nPerfecto... eso es.\nNecesito una calabaza, una vela... y encontrar ese árbol.",
            "[Enfant]\nAvec la peur... je n'avais pas vu ce livre.\n« Mausolée... arbre... gardien du repos... »\nUn arbre qui garde la tombe ? Hmm...\n« Seule la lumière d'une citrouille allumée révèle le lieu du repos. »\nDes citrouilles... comme celles de la cuisine... avec des visages et des bougies dedans.\nDonc... si je les allume, je pourrai voir où la sorcière est enterrée.\n« Sa lumière tient les esprits de l'arbre à distance. »\nParfait... c'est ça.\nIl me faut une citrouille, une bougie... et trouver cet arbre."),

        ("20",
            null,
            "[Niña]\nBrrrrr... el aire está más frío.\n¿Qué es eso... entre la niebla?...\nAhí está: el árbol.\nEs enorme... solo tengo que ver cómo abrir esta vieja reja, cubierta de raíces... está atascada.",
            "[Enfant]\nBrrrrr... l'air est plus froid.\nC'est quoi, ça... dans le brouillard ?...\nLe voilà : l'arbre.\nIl est immense... je dois juste trouver comment ouvrir cette vieille grille, couverte de racines... elle est coincée."),

        ("21",
            null,
            "[Niña]\nUf... este árbol da miedo: casi parece que tiene cara.\nMmm... encenderé la calabaza, como decía el libro.\n“La luz de una calabaza encendida revela el lugar del descanso.”",
            "[Enfant]\nOuah... cet arbre fait peur : on dirait presque qu'il a un visage.\nHmm... je vais allumer la citrouille, comme le disait le livre.\n« La lumière d'une citrouille allumée révèle le lieu du repos. »"),

        ("22",
            null,
            "[Niña]\nUf... este árbol da miedo: casi parece que tiene cara.\nMmm... encenderé la calabaza, como decía el libro.\n“La luz de una calabaza encendida revela el lugar del descanso.”",
            "[Enfant]\nOuah... cet arbre fait peur : on dirait presque qu'il a un visage.\nHmm... je vais allumer la citrouille, comme le disait le livre.\n« La lumière d'une citrouille allumée révèle le lieu du repos. »"),

        ("23",
            null,
            W + "\n[Bruja]\n¡Devuélveme lo que es mío!" + Wc + "\n\n[Niña]\n¡No te acercarás! ¡Tengo mi calabaza aquí mismo! ¡Toma esto!\n\n" + W + "\n[Bruja]\n¡Esa luz! ¡Apágala!" + Wc + "\n\n[Niña]\nQuédate ahí... no te acerques más...",
            W + "\n[Sorcière]\nRends-moi ce qui m'appartient !" + Wc + "\n\n[Enfant]\nTu n'approcheras pas ! J'ai ma citrouille juste ici ! Prends ça !\n\n" + W + "\n[Sorcière]\nCette lumière ! Éteins-la !" + Wc + "\n\n[Enfant]\nReste là... n'approche pas..."),

        ("25",
            null,
            "[Niña]\nVale... allá voy...\nPondré la mano dentro...\nPor favor... que funcione.\n¿Eh?...\nTodo... está en silencio.\nLa bruja...\nya no grita... ni está enfadada...",
            "[Enfant]\nBon... c'est parti...\nJe vais mettre la main à l'intérieur...\nS'il te plaît... que ça marche.\nHein ?...\nTout... est calme maintenant.\nLa sorcière...\nelle ne crie plus... elle n'est plus en colère..."),

        ("26",
            "<color=#BEFF7A>\n[Witch]\nThank you, child... I don’t like waking up.\nSomeone took my hand... and forced me to wander.</color>\n\n[Child]\nDon’t worry, miss. Let’s go back to the house... I’ll make you some tea... and you can go back to sleep, peacefully.",
            W + "\n[Bruja]\nGracias, niña... no me gusta despertarme.\nAlguien se llevó mi mano... y me obligó a vagar." + Wc + "\n\n[Niña]\nNo se preocupe, señora. Volvamos a la casa... le prepararé un té... y podrá volver a dormir, en paz.",
            W + "\n[Sorcière]\nMerci, petite... je n'aime pas me réveiller.\nQuelqu'un a pris ma main... et m'a forcée à errer." + Wc + "\n\n[Enfant]\nNe vous inquiétez pas, madame. Retournons à la maison... je vous préparerai un thé... et vous pourrez vous rendormir, en paix."),

        ("27",
            null,
            "[Niña]\nYa está... ¿mejor así? Puse unas flores para decorar.\n\n" + W + "\n[Bruja]\nGracias, niña...\nSiento haberte asustado tanto. No era mi intención." + Wc + "\n\n[Niña]\nBueno... no todos los días conoces a una bruja que le tiene miedo a las arañas.\n\n" + W + "\n[Bruja]\nSupongo que no." + Wc,
            "[Enfant]\nVoilà... c'est mieux comme ça ? J'ai mis quelques fleurs pour décorer.\n\n" + W + "\n[Sorcière]\nMerci, petite...\nJe suis désolée de t'avoir fait si peur. Ce n'était pas mon intention." + Wc + "\n\n[Enfant]\nEh bien... ce n'est pas tous les jours qu'on rencontre une sorcière qui a peur des araignées.\n\n" + W + "\n[Sorcière]\nJ'imagine que non." + Wc),

        ("28",
            null,
            "[Niña]\nPrometo que no entraré en su casa sin permiso.\n\n" + W + "\n[Bruja]\nMichifús se enteraría." + Wc + "\n\n[Niña]\nQue descanse. Y... gracias por no convertirme en rana.\n\n" + W + "\n[Bruja]\n¡Ja! ¡Esa ni se me había ocurrido!" + Wc,
            "[Enfant]\nJe promets de ne plus entrer chez vous sans permission.\n\n" + W + "\n[Sorcière]\nMittens le saurait." + Wc + "\n\n[Enfant]\nDormez bien. Et... merci de ne pas m'avoir transformée en grenouille.\n\n" + W + "\n[Sorcière]\nHa ! Je n'y avais même pas pensé !" + Wc),

        ("29",
            null,
            "[Niña]\n¿Michifús?...\nAsí que sigues por aquí, ¿eh?\nNo creas que he olvidado todos los sustos que me diste, bola de pelos... casi me da un infarto.\nVale, vale... paz entre nosotros.\nTe traeré comida todos los días, ¿trato?\nPero prométeme que no me delatarás la próxima vez que haya una bruja cerca.\nTrato hecho, Michifús.",
            "[Enfant]\nMittens ?...\nAlors tu es toujours dans le coin, hein ?\nNe crois pas que j'ai oublié toutes les frayeurs que tu m'as faites, petite boule de poils... j'ai failli avoir une crise cardiaque.\nBon, bon... la paix entre nous.\nJe t'apporterai à manger tous les jours, marché conclu ?\nMais promets-moi de ne pas me trahir la prochaine fois qu'il y aura une sorcière dans les parages.\nMarché conclu, Mittens."),
    };

    [MenuItem("Tremblay/Localización/Fase 5 - Rellenar historia en es y fr")]
    public static void RellenarTraducciones()
    {
        var textos = LocalizationEditorSettings.GetStringTableCollection(TablaStory);
        if (textos == null) { Debug.LogError("[Fase 5] Falta la tabla Story."); return; }

        int nuevas = 0, corregidas = 0;
        var faltan = new System.Collections.Generic.List<string>();

        foreach (var (id, en, es, fr) in Traducciones)
        {
            string clave = $"story.{id}";
            if (textos.SharedData.GetEntry(clave) == null) { faltan.Add(clave); continue; }

            if (en != null) corregidas += Sobrescribir(textos, "en", clave, en);
            nuevas += PonerSiVacio(textos, "es", clave, es);
            nuevas += PonerSiVacio(textos, "fr", clave, fr);
        }

        EditorUtility.SetDirty(textos.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Fase 5] Historia: {nuevas} valores rellenados, {corregidas} correcciones en en." +
                  (faltan.Count > 0 ? $" Sin clave ({faltan.Count}): {string.Join(", ", faltan)}" : ""));
    }

    static int PonerSiVacio(StringTableCollection col, string locale, string clave, string valor)
    {
        var tabla = col.GetTable(locale) as StringTable;
        if (tabla == null) return 0;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (string.IsNullOrEmpty(entrada.Value)) entrada.Value = valor;
        else return 0;

        EditorUtility.SetDirty(tabla);
        return 1;
    }

    static int Sobrescribir(StringTableCollection col, string locale, string clave, string valor)
    {
        var tabla = col.GetTable(locale) as StringTable;
        if (tabla == null) return 0;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (entrada.Value == valor) return 0;
        else entrada.Value = valor;

        EditorUtility.SetDirty(tabla);
        return 1;
    }

    static bool EsPlaceholder(string s)
    {
        string t = s.Trim();
        return t.Length == 0 || t.Replace("a", "").Length == 0;
    }
}
