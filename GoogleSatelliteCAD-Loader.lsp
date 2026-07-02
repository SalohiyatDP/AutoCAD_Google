;;; =====================================================================================
;;;  GoogleSatelliteCAD — Avtomatik yuklash (AutoLISP loader)
;;; =====================================================================================
;;;
;;;  O'RNATISH (bir marta):
;;;    1. Pastdagi GSATCAD_DLL_PATH qatorida DLL yo'lini TO'G'RILANG
;;;       (o'zingizning kompyuteringizdagi to'liq yo'l)
;;;    2. AutoCAD oching → APPLOAD
;;;    3. "Приложения" (Startup Suite) → "Добавить" → shu .lsp ni tanlang
;;;    4. Tayyor! AutoCAD har safar ochilganda plagin yuklanadi.
;;;
;;; =====================================================================================

;;; ===== SHU QATORNI O'ZGARTIRING: DLL ning TO'LIQ YO'LINI YOZING =====
(setq GSATCAD_DLL_PATH "C:\\GoogleSatelliteCAD\\bin\\x64\\Release\\GoogleSatelliteCAD.dll")
;;; =====================================================================

(defun gsatcad-autoload ( / dllpath)
  (setq dllpath GSATCAD_DLL_PATH)

  (if (findfile dllpath)
    (progn
      (command "._NETLOAD" dllpath)
      (princ (strcat "\n[GoogleSatelliteCAD] Yuklandi: " dllpath))
    )
    (princ (strcat "\n[GoogleSatelliteCAD] XATOLIK: DLL topilmadi: " dllpath
                   "\n  .lsp fayldagi GSATCAD_DLL_PATH qatorini tekshiring!"))
  )
  (princ)
)

;;; Avtomatik ishga tushadi:
(gsatcad-autoload)
