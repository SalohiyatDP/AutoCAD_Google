;;; =====================================================================================
;;;  GoogleSatelliteCAD — Avtomatik yuklash (AutoLISP loader)
;;; =====================================================================================
;;;
;;;  O'RNATISH (bir marta):
;;;    1. GoogleSatelliteCAD.dll va shu .lsp faylni BITTA papkaga joylashtiring.
;;;       Masalan: C:\GoogleSatelliteCAD\
;;;                ├── GoogleSatelliteCAD.dll
;;;                └── GoogleSatelliteCAD-Loader.lsp
;;;
;;;    2. AutoCAD oching → APPLOAD (yoki Меню → Tools → Load Application)
;;;    3. "Приложения" (Startup Suite) tugmasini bosing (pastki o'ng burchakda)
;;;    4. "Добавить" (Add) → shu .lsp faylni tanlang
;;;    5. Oynani yoping. Tayyor!
;;;
;;;  Endi AutoCAD har safar ochilganda plagin AVTOMATIK yuklanadi.
;;;
;;; =====================================================================================

(defun gsatcad-autoload ( / thisfile thisdir dllpath)

  ;; 1. Shu .lsp fayl qayerda joylashganini aniqlaymiz
  (setq thisfile (findfile "GoogleSatelliteCAD-Loader.lsp"))

  (if thisfile
    (progn
      ;; 2. Papka yo'lini olamiz
      (setq thisdir (vl-filename-directory thisfile))
      ;; 3. DLL to'liq yo'lini quramiz
      (setq dllpath (strcat thisdir "\\GoogleSatelliteCAD.dll"))

      ;; 4. DLL borligini tekshiramiz va yuklaymiz
      (if (findfile dllpath)
        (progn
          (command "._NETLOAD" dllpath)
          (princ (strcat "\n[GoogleSatelliteCAD] Yuklandi: " dllpath))
        )
        ;; DLL topilmadi — xabar beramiz
        (princ (strcat "\n[GoogleSatelliteCAD] XATOLIK: DLL topilmadi!\n  Kutilgan joy: " dllpath
                       "\n  DLL va .lsp fayllar BITTA papkada bo'lishi kerak."))
      )
    )
    ;; .lsp fayl o'zi topilmadi (kutilmagan holat)
    (princ "\n[GoogleSatelliteCAD] XATOLIK: Loader fayli topilmadi.")
  )
  (princ)
)

;;; Fayl yuklanganda avtomatik ishga tushadi:
(gsatcad-autoload)
