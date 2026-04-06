using System.Collections.Generic;
using Zakarni.Core.Models;

namespace Zakarni.Core.Services;

public class AthkarService
{
    public List<AthkarItem> GetMorningAthkar() => new()
    {
        new AthkarItem { Text = "أصبحنا وأصبح الملك لله، والحمد لله، لا إله إلا الله وحده لا شريك له، له الملك وله الحمد وهو على كل شيء قدير", Description = "مرة واحدة", Count = 1, Order = 1 },
        new AthkarItem { Text = "اللهم بك أصبحنا، وبك أمسينا، وبك نحيا، وبك نموت، وإليك النشور", Description = "مرة واحدة", Count = 1, Order = 2 },
        new AthkarItem { Text = "اللهم أنت ربي لا إله إلا أنت، خلقتني وأنا عبدك، وأنا على عهدك ووعدك ما استطعت، أعوذ بك من شر ما صنعت، أبوء لك بنعمتك علي، وأبوء بذنبي فاغفر لي فإنه لا يغفر الذنوب إلا أنت", Description = "سيد الاستغفار - من قالها موقنا بها حين يصبح فمات من يومه قبل أن يمسي فهو من أهل الجنة", Count = 1, Order = 3 },
        new AthkarItem { Text = "اللهم إني أصبحت أشهدك وأشهد حملة عرشك، وملائكتك وجميع خلقك، أنك أنت الله لا إله إلا أنت وحدك لا شريك لك، وأن محمداً عبدك ورسولك", Description = "أربع مرات - من قالها أعتقه الله من النار", Count = 4, Order = 4 },
        new AthkarItem { Text = "سُبْحَانَ اللهِ وَبِحَمْدِهِ", Description = "مائة مرة", Count = 100, Order = 5 },
        new AthkarItem { Text = "يا حي يا قيوم برحمتك أستغيث أصلح لي شأني كله ولا تكلني إلى نفسي طرفة عين", Description = "مرة واحدة", Count = 1, Order = 6 }
    };

    public List<AthkarItem> GetEveningAthkar() => new()
    {
        new AthkarItem { Text = "أمسينا وأمسى الملك لله، والحمد لله، لا إله إلا الله وحده لا شريك له، له الملك وله الحمد وهو على كل شيء قدير", Description = "مرة واحدة", Count = 1, Order = 1 },
        new AthkarItem { Text = "اللهم بك أمسينا، وبك أصبحنا، وبك نحيا، وبك نموت، وإليك المصير", Description = "مرة واحدة", Count = 1, Order = 2 },
        new AthkarItem { Text = "اللهم أنت ربي لا إله إلا أنت، خلقتني وأنا عبدك، وأنا على عهدك ووعدك ما استطعت، أعوذ بك من شر ما صنعت، أبوء لك بنعمتك علي، وأبوء بذنبي فاغفر لي فإنه لا يغفر الذنوب إلا أنت", Description = "سيد الاستغفار", Count = 1, Order = 3 },
        new AthkarItem { Text = "أعوذ بكلمات الله التامات من شر ما خلق", Description = "ثلاث مرات", Count = 3, Order = 4 },
        new AthkarItem { Text = "اللهم إني أعوذ بك من الهم والحزن، والعجز والكسل، والبخل والجبن، وضلع الدين، وغلبة الرجال", Description = "مرة واحدة", Count = 1, Order = 5 }
    };

    public List<AthkarItem> GetAfterPrayerAthkar() => new()
    {
        new AthkarItem { SubCategory = "All", Text = "أستغفر الله (ثلاثاً) اللهم أنت السلام ومنك السلام تباركت يا ذا الجلال والإكرام", Description = "بعد الصلاة مباشرة", Count = 1, Order = 1 },
        new AthkarItem { SubCategory = "All", Text = "سبحان الله (33)، والحمد لله (33)، والله أكبر (33)، ثم تمام المائة: لا إله إلا الله وحده لا شريك له، له الملك وله الحمد وهو على كل شيء قدير", Description = "بعد كل صلاة", Count = 1, Order = 2 },
        new AthkarItem { SubCategory = "Fajr", Text = "اللهم إني أسألك علماً نافعاً، ورزقاً طيباً، وعملاً متقبلاً", Description = "بعد صلاة الفجر", Count = 1, Order = 3 },
        new AthkarItem { SubCategory = "Maghrib", Text = "لا إله إلا الله وحده لا شريك له، له الملك وله الحمد، يحيي ويميت وهو على كل شيء قدير", Description = "عشر مرات بعد المغرب", Count = 10, Order = 4 }
    };
}
