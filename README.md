## Реализовано в коде

-   Команды и урон:  `CombatTeam`,  `TeamMember`,  `Health`  (дружественный огонь отключён).
-   Стрельба:  `HitscanWeapon`  — луч, выбор ближайшего валидного  `Health`, сброс кулдауна между раундами.
-   Управление:  `FpsMotor`,  `FpsLook`,  `FpsInputDriver`  — читает карту  Player  из  `InputSystem_Actions`  (поле  `actions`  в инспекторе).
-   Раунды:  `RoundOutcomeResolver`  (вылет / таймер с суммой HP),  `RoundManager`  (счёт, пауза между раундами, респавн через  `SpawnPoint`).
-   HUD:  `HudController`  (три  `UnityEngine.UI.Text`: таймер, счёт, баннер).
-   Боты:  `FpsMobController`  —  `NavMeshAgent`, поиск противника команды, наведение,  `TryManualFire`  в пределах дистанции.
-   Редактор:  меню  `FPS Shooter`  →  `Drop Empty Round Manager`  — пустой объект с  `RoundManager`  (ссылки вешаются вручную).

Файлы лежат под  `FPS Shooter/Assets/Scripts/Game/...`, добавлен  `FPS Shooter/.gitignore`, изменения закоммичены.

## Что нужно собрать в сцене (вручную)

1.  Пол  (куб / плоскость), отметить  Static, при необходимости отметить как  Walkable  в окне Navigation.
2.  Bake Nav Mesh  (Window ▸ AI ▸ Navigation или Navigation overlay) по полу — иначе боты не пойдут.
3.  Игрок:  капсула +  `CharacterController`,  `TeamMember`  (ALPHA),  `Health`,  `FpsMotor`,  `FpsLook`  (якорь поворота = тело,  `pitchPivot`  — дочерний pivot на уровень головы ~1.55–1.6). На дочернюю камеру:  `Camera`,  `AudioListener`,  `HitscanWeapon`, поле  `wielderTeam`  → тот же  `TeamMember`.
4.  `FpsInputDriver`  на тело игрока:  `actions`  =  `Assets/InputSystem_Actions`, ссылки на  `motor`,  `look`,  `weapon`  (камера).
5.  Боты:  капсула +  `CapsuleCollider`,  `NavMeshAgent`,  `TeamMember`  (ALPHA/BETA),  `Health`, на дочернем узле камеры/`HitscanWeapon`  как у плана +  `FpsMobController`  (`affiliation`,  `primary`).
6.  `SpawnPoint`  (4 точки минимум, 2 ALPHA + 2 BETA или больше) — только пустышки на полу с компонентом  `SpawnPoint`.
7.  HUD:  Canvas + три  `Text`, объект с  `HudController`, привязать поля текстов (`BindOptional`  уже можно вызвать из скрипта или заполнить в инспекторе).
8.  `RoundManager`:  перетащить  Hud, массив  всех  `Health`  в бою и массив  SpawnPoint. Параметры: длительность раунда, пауза между раундами,  wins до победы в матче  (например 4).

Пока  `SampleScene`  не содержит  этих объектов — код готов к подключению; быстрый старт можно сделать через  `FPS Shooter`  →  `Drop Empty Round Manager`, затем расставить персонажей и связи по пунктам выше.

Если нужно, в следующей итерации можно добавить маленький редакторский «генератор сцены» (пол + игрок + боты + HUD одной кнопкой) уже поверх этой базы — напишите, сделаю аккуратно и без обрыва кода, как чуть раньше в черновике.
