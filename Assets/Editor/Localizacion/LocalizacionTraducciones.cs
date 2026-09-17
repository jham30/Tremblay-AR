using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Traducciones es/fr de los labels de escena y de los pasos del tutorial que la Fase 2 dejó
/// solo en inglés. No pisa valores ya escritos: si retocas algo en la tabla, se respeta.
/// </summary>
public static class LocalizacionTraducciones
{
    const string TablaUI = "UI";

    // La columna 'en' se sembró con el texto tal cual estaba en la escena, typos incluidos.
    static readonly (string clave, string en)[] CorreccionesEn =
    {
        ("ui.setings", "Settings"),
        ("ui.reset_al", "Reset All"),
        ("ui.skip_tuitorial", "Skip Tutorial"),
        ("ui.abandonar", "Drop it"),
        ("tutorial.step.08", "8. Did you hear how it's said in {0}? If you want to hear it again, tap the button I'm pointing at."),
        ("tutorial.step.36", "35. That's everything! Now go have fun and learn a bit of {0} with me. See you in the game!"),
    };

    static readonly (string clave, string es, string fr)[] Traducciones =
    {
        // --- Labels de escena ---
        ("ui.abandonar", "Soltar", "Lâcher"),
        ("ui.about_us", "Acerca de", "À propos"),
        ("ui.begin_adventure", "Empezar la aventura", "Commencer l'aventure"),
        ("ui.check", "Comprobar", "Vérifier"),
        ("ui.close", "Cerrar", "Fermer"),
        ("ui.continue", "Continuar", "Continuer"),
        ("ui.done", "¡Listo!", "Terminé !"),
        // Los nombres de idioma van siempre en su propio idioma.
        ("ui.english", "English", "English"),
        ("ui.espanol", "Español", "Español"),
        ("ui.francais", "Français", "Français"),
        ("ui.get_it", "Guardar", "Prendre"),
        ("ui.mission_reset", "Reiniciar misiones", "Réinitialiser les missions"),
        ("ui.music_off", "Música: apagada", "Musique : coupée"),
        ("ui.next_step", "Siguiente paso", "Étape suivante"),
        ("ui.objects_reset", "Reiniciar objetos", "Réinitialiser les objets"),
        ("ui.place_it", "Colocar", "Poser"),
        ("ui.repeat_tutorial", "Repetir tutorial", "Refaire le tutoriel"),
        ("ui.reset", "Reiniciar", "Réinitialiser"),
        ("ui.reset_al", "Reiniciar todo", "Tout réinitialiser"),
        ("ui.reset_game", "Reiniciar juego", "Réinitialiser le jeu"),
        ("ui.setings", "Configuración", "Paramètres"),
        ("ui.settings", "Configuración", "Paramètres"),
        ("ui.skip", "Saltar", "Passer"),
        ("ui.skip_tuitorial", "Saltar tutorial", "Passer le tutoriel"),
        ("ui.sound_effects_off", "Efectos: apagados", "Effets sonores : coupés"),
        ("ui.yes_reset", "¡Sí, reiniciar!", "Oui, réinitialiser !"),

        // --- Tutorial (idioma nativo; {0} = nombre del idioma meta) ---
        ("tutorial.step.01",
            "1. ¡Hola! Soy Éliane Tremblay, y esta es MI aventura en la casa de la calle Hollow. Te voy a enseñar a jugar, ¿vale? ¡Vamos a pasarlo genial!",
            "1. Salut ! Je suis Éliane Tremblay, et voici MON aventure dans la maison de la rue Hollow. Je vais t'apprendre à jouer, d'accord ? On va bien s'amuser !"),
        ("tutorial.step.02",
            "2. Este juego usa Realidad Aumentada, o sea que vamos a ver todo a través de la cámara de tu teléfono. ¡Es como abrir una ventana mágica a la historia!",
            "2. Ce jeu utilise la Réalité Augmentée, ce qui veut dire qu'on va tout voir à travers la caméra de ton téléphone. C'est comme ouvrir une fenêtre magique sur l'histoire !"),
        ("tutorial.step.03",
            "3. Un momento... estoy despertando la cámara mágica.",
            "3. Un instant... je réveille la caméra magique."),
        ("tutorial.step.04",
            "4. Este juego funciona con tarjetas especiales. Tienes que tenerlas impresas o en otra pantalla. Si todavía no las tienes, descárgalas en thetremblay.com/downloads",
            "4. Ce jeu fonctionne avec des cartes spéciales. Il faut les avoir imprimées, ou sur un autre écran. Si tu ne les as pas encore, télécharge-les sur thetremblay.com/downloads"),
        ("tutorial.step.05",
            "5. Busca las 2 tarjetas que dicen \"TUTORIAL\". Esas son las que vamos a usar ahora. Apunta la cámara a una de ellas, como si fueras a tomarle una foto.",
            "5. Cherche les 2 cartes où il est écrit « TUTORIAL ». Ce sont celles qu'on va utiliser maintenant. Pointe ta caméra vers l'une d'elles, comme si tu allais la prendre en photo."),
        ("tutorial.step.06",
            "6. ¡Mira! Apareció una vela <sprite name=\"vela\"> sobre la tarjeta.",
            "6. Regarde ! Une bougie <sprite name=\"vela\"> est apparue sur la carte."),
        ("tutorial.step.07",
            "7. Puedes tocar los objetos directamente en la pantalla cuando aparecen. ¡Pruébalo! Toca la vela <sprite name=\"vela\">.",
            "7. Tu peux toucher les objets directement sur l'écran quand ils apparaissent. Essaie ! Touche la bougie <sprite name=\"vela\">."),
        ("tutorial.step.08",
            "8. ¿Oíste cómo se dice en {0}? Si quieres escucharlo otra vez, toca el botón que te señalo.",
            "8. Tu as entendu comment on le dit en {0} ? Si tu veux l'écouter encore, touche le bouton que je te montre."),
        ("tutorial.step.09",
            "9. Puedes escucharlo tantas veces como quieras. ¡Yo también lo hacía cuando estaba aprendiendo! Solo toca el botón otra vez.",
            "9. Tu peux l'écouter autant de fois que tu veux. Moi aussi je le faisais quand j'apprenais ! Touche simplement le bouton encore une fois."),
        ("tutorial.step.10",
            "10. Ahora vamos a guardar este objeto para usarlo después. Toca el botón que te muestro para guardarlo en tu inventario.",
            "10. Maintenant, on va garder cet objet pour l'utiliser plus tard. Touche le bouton que je te montre pour le mettre dans ton inventaire."),
        ("tutorial.step.11",
            "11. ¡Genial, ahora es tuyo!",
            "11. Super, il est à toi maintenant !"),
        ("tutorial.step.12",
            "12. Ahora puedes cerrarlo tocando aquí. Recuerda: cuando quieras volver a ver un objeto, solo apunta la cámara hacia él y tócalo.",
            "12. Maintenant tu peux le fermer en touchant ici. N'oublie pas : quand tu veux revoir un objet, pointe simplement la caméra vers lui et touche-le."),
        ("tutorial.step.13",
            "13. Ahora guarda también el otro objeto que apareció. Apunta la cámara a la otra tarjeta que encontraste.",
            "13. Vas-y, garde aussi l'autre objet qui est apparu. Pointe ta caméra vers l'autre carte que tu as trouvée."),
        ("tutorial.step.14",
            "14. ¡Oye, veo que encontraste la calabaza! <sprite name=\"calabaza\"> Es una de mis favoritas.",
            "14. Hé, je vois que tu as trouvé la citrouille ! <sprite name=\"calabaza\"> C'est l'une de mes préférées."),
        ("tutorial.step.15",
            "15. Guárdala en tu inventario, igual que hicimos con la vela <sprite name=\"vela\">.",
            "15. Mets-la dans ton inventaire, comme on l'a fait avec la bougie <sprite name=\"vela\">."),
        ("tutorial.step.16",
            "16. ¡Perfecto! Ahora cierra el panel. Quiero enseñarte algo muy importante.",
            "16. Parfait ! Maintenant ferme le panneau. Je veux te montrer quelque chose de très important."),
        ("tutorial.step.17",
            "17. ¿Ves la calabaza <sprite name=\"calabaza\"> ahí abajo? Te la estoy señalando. Puedes tocarla, o deslizar hacia arriba para abrir tu inventario. ¡Pruébalo!",
            "17. Tu vois la citrouille <sprite name=\"calabaza\"> en bas ? Je te la montre. Tu peux la toucher, ou glisser vers le haut pour ouvrir ton inventaire. Essaie !"),
        ("tutorial.step.18",
            "18. ¡Mira! Aquí están los dos objetos que guardaste: la vela <sprite name=\"vela\"> y la calabaza <sprite name=\"calabaza\">. Ahora son tuyos.",
            "18. Regarde ! Voici les deux objets que tu as gardés : la bougie <sprite name=\"vela\"> et la citrouille <sprite name=\"calabaza\">. Ils sont à toi maintenant."),
        ("tutorial.step.19",
            "19. Arriba hay una misión. Sé que quizá todavía no entiendas del todo lo que dice, pero seguro que adivinas qué objetos pide.",
            "19. En haut, il y a une mission. Je sais que tu ne comprends peut-être pas encore tout ce qu'elle dit, mais je parie que tu devines quels objets elle demande."),
        ("tutorial.step.20",
            "20. Arrastra la vela <sprite name=\"vela\"> al lugar donde va. Si no recuerdas cómo se dice \"vela <sprite name=\"vela\">\", toca el botón verde que tiene al lado para escucharlo otra vez.",
            "20. Glisse la bougie <sprite name=\"vela\"> à l'endroit où elle va. Si tu ne te souviens plus comment on dit « bougie <sprite name=\"vela\"> », touche le bouton vert à côté pour l'écouter encore."),
        ("tutorial.step.21",
            "21. ¡Así se hace!",
            "21. C'est comme ça qu'on fait !"),
        ("tutorial.step.22",
            "22. Ahora arrastra la vela <sprite name=\"vela\"> y la calabaza <sprite name=\"calabaza\">, cada una a su lugar.",
            "22. Maintenant glisse la bougie <sprite name=\"vela\"> et la citrouille <sprite name=\"calabaza\">, chacune à sa place."),
        ("tutorial.step.23",
            "23. ¡Lo estás haciendo muy bien! Ahora toca el botón que te señalo para comprobar si las pusiste bien.",
            "23. Tu te débrouilles très bien ! Maintenant touche le bouton que je te montre pour vérifier si tu les as bien placées."),
        ("tutorial.step.24",
            "24. ¡Síii! Acabas de descifrar la misión. ¿Adivinas qué dice? No te preocupes si no: con el tiempo entenderás más palabras. Por ahora, ¡vamos a completarla!",
            "24. Ouiii ! Tu viens de déchiffrer la mission. Tu devines ce qu'elle dit ? Ne t'inquiète pas sinon : avec le temps, tu comprendras plus de mots. Pour l'instant, allons la terminer !"),
        ("tutorial.step.25",
            "25. Ahora sí, vamos a llevar la vela <sprite name=\"vela\"> hasta la calabaza. Pero primero cierra el inventario deslizando hacia abajo o tocando el botón que te señalo.",
            "25. Maintenant, apportons vraiment la bougie <sprite name=\"vela\"> jusqu'à la citrouille. Mais d'abord, ferme l'inventaire en glissant vers le bas ou en touchant le bouton que je te montre."),
        ("tutorial.step.26",
            "26. Ahora escanea la tarjeta de la vela <sprite name=\"vela\"> con tu cámara. ¿Recuerdas cuál es?",
            "26. Maintenant scanne la carte de la bougie <sprite name=\"vela\"> avec ta caméra. Tu te souviens laquelle c'est ?"),
        ("tutorial.step.27",
            "27. ¿Viste que apareció una mano? Tócala para agarrar la vela.",
            "27. Tu as vu qu'une main est apparue ? Touche-la pour attraper la bougie."),
        ("tutorial.step.28",
            "27a. Cierra el panel de la vela.",
            "27a. Ferme le panneau de la bougie."),
        ("tutorial.step.29",
            "28. ¡La vela <sprite name=\"vela\"> ya está contigo! Ve a la tarjeta de la calabaza <sprite name=\"calabaza\"> y apúntala.",
            "28. La bougie <sprite name=\"vela\"> est avec toi maintenant ! Va vers la carte de la citrouille <sprite name=\"calabaza\"> et pointe-la."),
        ("tutorial.step.30",
            "29. Aquí también apareció una mano, ¿la ves? Eso significa que puedes soltar la vela <sprite name=\"vela\"> sobre la calabaza <sprite name=\"calabaza\">.",
            "29. Une main est apparue ici aussi, tu la vois ? Ça veut dire que tu peux poser la bougie <sprite name=\"vela\"> sur la citrouille <sprite name=\"calabaza\">."),
        ("tutorial.step.31",
            "30. Ya casi. Ahora toca el botón que te señalo para comprobar que trajiste lo que pedía la misión.",
            "30. On y est presque. Maintenant touche le bouton que je te montre pour vérifier que tu as apporté ce que demandait la mission."),
        ("tutorial.step.32",
            "32. ¿Ahora sabes qué pedía la misión? Eso es: poner la vela <sprite name=\"vela\"> dentro de la calabaza <sprite name=\"calabaza\">. Creo que ya estás listo para jugar por tu cuenta.",
            "32. Tu sais maintenant ce que demandait la mission ? Exactement : mettre la bougie <sprite name=\"vela\"> dans la citrouille <sprite name=\"calabaza\">. Je crois que tu es prêt à jouer tout seul."),
        ("tutorial.step.33",
            "33. ¡Casi lo olvido! ¿Ves ese icono verde? Tócalo para ver todas tus misiones. Ahora mismo solo hay una, pero cuando empieces a jugar de verdad, ¡aparecerán muchísimas más!",
            "33. J'ai failli oublier ! Tu vois cette icône verte ? Touche-la pour voir toutes tes missions. Pour l'instant il n'y en a qu'une, mais quand tu joueras pour de vrai, il y en aura plein d'autres !"),
        ("tutorial.step.34",
            "34. Una cosa más: las misiones cambian de color según su estado. 🔴 Rojo: todavía no disponible. 🔵 Azul: lista para descifrar. 🟡 Amarillo: ya descifrada, falta completarla con la cámara. 🟢 Verde: ¡completada!",
            "34. Une dernière chose : les missions changent de couleur selon leur état. 🔴 Rouge : pas encore disponible. 🔵 Bleu : prête à déchiffrer. 🟡 Jaune : déjà déchiffrée, il reste à la terminer avec la caméra. 🟢 Vert : terminée !"),
        ("tutorial.step.35",
            "34a. Cierra la lista de misiones.",
            "34a. Ferme la liste des missions."),
        ("tutorial.step.36",
            "35. ¡Eso es todo! Ahora ve a divertirte y a aprender un poco de {0} conmigo. ¡Nos vemos en el juego!",
            "35. C'est tout ! Maintenant va t'amuser et apprendre un peu de {0} avec moi. On se retrouve dans le jeu !"),
    };

    [MenuItem("Tremblay/Localización/Rellenar traducciones es y fr que falten")]
    public static void Rellenar()
    {
        var coleccion = LocalizationEditorSettings.GetStringTableCollection(TablaUI);
        if (coleccion == null)
        {
            Debug.LogError($"[Localización] No existe la tabla '{TablaUI}'.");
            return;
        }

        int corregidas = 0, nuevas = 0, faltan = 0;

        foreach (var (clave, en) in CorreccionesEn)
            corregidas += Sobrescribir(coleccion, "en", clave, en);

        foreach (var (clave, es, fr) in Traducciones)
        {
            if (coleccion.SharedData.GetEntry(clave) == null) { faltan++; Debug.LogWarning($"[Localización] La clave '{clave}' no existe en la tabla; se salta."); continue; }
            nuevas += PonerSiVacio(coleccion, "es", clave, es);
            nuevas += PonerSiVacio(coleccion, "fr", clave, fr);
        }

        EditorUtility.SetDirty(coleccion.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Localización] Traducciones: {nuevas} valores rellenados, {corregidas} correcciones en 'en', {faltan} claves inexistentes.");
    }

    static int PonerSiVacio(StringTableCollection coleccion, string locale, string clave, string valor)
    {
        var tabla = coleccion.GetTable(locale) as StringTable;
        if (tabla == null) return 0;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (string.IsNullOrEmpty(entrada.Value)) entrada.Value = valor;
        else return 0;

        EditorUtility.SetDirty(tabla);
        return 1;
    }

    static int Sobrescribir(StringTableCollection coleccion, string locale, string clave, string valor)
    {
        var tabla = coleccion.GetTable(locale) as StringTable;
        if (tabla == null || coleccion.SharedData.GetEntry(clave) == null) return 0;

        var entrada = tabla.GetEntry(clave);
        if (entrada == null) tabla.AddEntry(clave, valor);
        else if (entrada.Value == valor) return 0;
        else entrada.Value = valor;

        EditorUtility.SetDirty(tabla);
        return 1;
    }
}
