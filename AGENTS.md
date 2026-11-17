# Veilix çalışma kuralları

- İsteği karşılayan en küçük değişikliği yap. Yalnızca ilgili dosyaları ve gerekli bağımlılıkları incele. Mevcut bileşenleri ve kod düzenini kullan. İlgisiz refaktörleri kapsam dışında tut.
- Hata düzeltirken belirtileri ve ilgili kayıtları incele, mümkünse hatayı yeniden üret ve değişikliği saptanan nedene dayandır. Uygun olduğunda hedefli test veya derleme kontrolü çalıştır. Son yanıtta değişikliği, yapılan doğrulamayı ve varsa açık kalan sorunları kısaca belirt.
- Kütüphane veya API dokümantasyonu gerektiğinde önce projedeki sürümü kontrol et ve [Context7](https://github.com/upstash/context7) üzerinden o sürümü araştır. Context7 erişilebilir değilse veya ilgili API'yi kapsamıyorsa resmi dokümantasyona başvur.
- Geliştirme yaklaşımında [Superpowers](https://github.com/obra/superpowers) rehberini kullan. Çok adımlı işleri küçük ve doğrulanabilir parçalara ayır. Hatalarda sistematik tanıyı, tamamlanma bildiriminde test ve gözlem sonuçlarını esas al. Süreci işin boyutuna göre uygula.
