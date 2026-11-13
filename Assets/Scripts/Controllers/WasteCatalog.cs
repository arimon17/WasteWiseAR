using UnityEngine;

[System.Serializable]
public struct WasteCatalogEntry
{
    public string modelFile;
    public string itemName;
    public TrashCategory category;
    [TextArea] public string explanation;
}

public static class WasteCatalog
{
    public static readonly WasteCatalogEntry[] Entries =
    {
        new WasteCatalogEntry
        {
            modelFile = "Apple",
            itemName = "Apel",
            category = TrashCategory.Organik,
            explanation = "Apel sisa termasuk sampah organik yang mudah terurai dan bisa diolah menjadi kompos."
        },
        new WasteCatalogEntry
        {
            modelFile = "Banana",
            itemName = "Kulit Pisang",
            category = TrashCategory.Organik,
            explanation = "Kulit pisang adalah sampah organik yang kaya nutrisi untuk kompos dan pupuk tanaman."
        },
        new WasteCatalogEntry
        {
            modelFile = "Bread_Slice",
            itemName = "Sisa Roti",
            category = TrashCategory.Organik,
            explanation = "Roti yang sudah basi termasuk organik; pisahkan dari kemasan plastiknya sebelum dikompos."
        },
        new WasteCatalogEntry
        {
            modelFile = "Carrot",
            itemName = "Wortel",
            category = TrashCategory.Organik,
            explanation = "Sisa wortel termasuk sampah organik; potong kecil agar lebih cepat terurai menjadi kompos."
        },
        new WasteCatalogEntry
        {
            modelFile = "FishBone",
            itemName = "Tulang Ikan",
            category = TrashCategory.Organik,
            explanation = "Tulang ikan termasuk sampah organik; buang terpisah dan jangan dicampur dengan plastik."
        },
        new WasteCatalogEntry
        {
            modelFile = "Bottle1",
            itemName = "Botol Plastik",
            category = TrashCategory.Anorganik,
            explanation = "Botol plastik PET bernilai daur ulang; bilas, lepaskan tutup, lalu remukkan sebelum dikumpulkan."
        },
        new WasteCatalogEntry
        {
            modelFile = "bottle-ketchup",
            itemName = "Botol Saus",
            category = TrashCategory.Anorganik,
            explanation = "Botol saus bekas termasuk anorganik; bilas sisa sausnya lalu serahkan ke bank sampah."
        },
        new WasteCatalogEntry
        {
            modelFile = "can",
            itemName = "Kaleng Minuman",
            category = TrashCategory.Anorganik,
            explanation = "Kaleng aluminium mudah didaur ulang dan bernilai jual; bilas lalu pipihkan."
        },
        new WasteCatalogEntry
        {
            modelFile = "candy-bar-wrapper",
            itemName = "Bungkus Permen",
            category = TrashCategory.Anorganik,
            explanation = "Bungkus permen berbahan plastik sulit terurai; kumpulkan dengan sampah anorganik."
        },
        new WasteCatalogEntry
        {
            modelFile = "carton",
            itemName = "Kotak Karton",
            category = TrashCategory.Anorganik,
            explanation = "Kotak karton minuman berlapis plastik dan aluminium; bilas, lipat, lalu keringkan sebelum didaur ulang."
        },
        new WasteCatalogEntry
        {
            modelFile = "battery",
            itemName = "Baterai Bekas",
            category = TrashCategory.B3,
            explanation = "Baterai mengandung logam berat berbahaya; buang ke tempat sampah B3, jangan ke sampah biasa."
        },
        new WasteCatalogEntry
        {
            modelFile = "bottle-oil",
            itemName = "Botol Oli Bekas",
            category = TrashCategory.B3,
            explanation = "Oli bekas mengandung bahan berbahaya; tampung di wadah tertutup dan serahkan ke fasilitas B3."
        },
        new WasteCatalogEntry
        {
            modelFile = "lightbulb",
            itemName = "Lampu Bekas",
            category = TrashCategory.B3,
            explanation = "Lampu bekas dapat mengandung merkuri; bungkus rapat dan serahkan ke titik drop-off B3."
        },
        new WasteCatalogEntry
        {
            modelFile = "paint",
            itemName = "Kaleng Cat",
            category = TrashCategory.B3,
            explanation = "Cat sisa mengandung bahan kimia berbahaya; tutup rapat dan serahkan ke pengolahan limbah B3."
        },
        new WasteCatalogEntry
        {
            modelFile = "Poison",
            itemName = "Wadah Pestisida",
            category = TrashCategory.B3,
            explanation = "Pestisida adalah limbah B3; jangan dibuang ke saluran air, serahkan ke fasilitas pengolahan khusus."
        }
    };
}
