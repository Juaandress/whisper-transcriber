# Política de Privacidad

**Última actualización:** 17 de septiembre de 2026  
**Producto:** Whisper Desktop / Whisper Transcriber  
**Editor:** Juaandress

Esta Política describe cómo se trata la información al usar la Aplicación. Está pensada para ser clara y alineada con prácticas habituales de software de escritorio local / open source.

---

## 1. Resumen

- **No pedimos cuenta** ni registro.
- **No vendemos datos**.
- La transcripción se hace **en tu PC**.
- **No enviamos** tus audios ni tus textos a nuestros servidores (no operamos un backend de transcripción).
- Puede haber conexiones a **terceros** solo para descargar la app o los modelos de IA (por ejemplo GitHub o Hugging Face).

---

## 2. Datos que procesamos (y dónde)

### 2.1 Archivos que vos elegís

Cuando seleccionás un audio o video, la Aplicación lo lee **localmente** para convertirlo y transcribirlo.  
Esos archivos **no se suben** a un servidor nuestro.

El texto resultante se guarda donde indiques (por defecto, en tu carpeta de Documentos).

### 2.2 Modelos de Whisper

La primera vez que usás un modelo, la Aplicación puede **descargarlo** desde repositorios públicos de modelos (p. ej. Hugging Face / ggml).  
Esa descarga es del archivo del modelo, no de tu contenido multimedia.

Los modelos se almacenan en tu equipo, típicamente bajo:

`%LocalAppData%\WhisperDesktop\models\`

### 2.3 Datos técnicos locales

La Aplicación puede crear archivos temporales (p. ej. WAV intermedios) en tu disco y borrarlos al terminar.  
No utilizamos esos temporales para perfilarte.

### 2.4 Lo que no recopilamos

En el diseño actual de Whisper Desktop **no** recopilamos de forma intencional:

- nombre, email o datos de cuenta,
- historial de transcripciones en la nube,
- publicidad basada en tu contenido,
- telemetría de analítica propia embebida en la app.

(Si en el futuro se agregara telemetría opcional, se documentará aquí y se pedirá consentimiento cuando corresponda.)

---

## 3. Terceros

Podés interactuar con terceros al:

| Acción | Tercero típico | Qué ocurre |
|--------|----------------|------------|
| Descargar el instalador | GitHub Releases | GitHub puede registrar IP, logs de descarga, cookies según su política |
| Descargar un modelo | Hugging Face u otro host de modelos | El tercero ve la solicitud de descarga del modelo (IP, user-agent, etc.) |
| Clonar el código | GitHub | Según la política de GitHub |

**No controlamos** las políticas de esos terceros. Revisá sus avisos de privacidad si necesitás detalle.

FFmpeg y Whisper.net/whisper.cpp se ejecutan en local una vez instalados/descargados.

---

## 4. Base legal / finalidad (orientativo)

Usamos el tratamiento local de tus archivos **solo** para prestar la función que pediste: transcribir.  
La descarga de modelos es necesaria para que esa función pueda ejecutarse offline después.

---

## 5. Conservación

- Tus media y `.txt` permanecen donde vos los guardes; nosotros no los alojamos.
- Podés borrar modelos y temporales borrando la carpeta `%LocalAppData%\WhisperDesktop\` y/o desinstalando la app.

---

## 6. Seguridad

Tomamos medidas razonables de diseño (procesamiento local, sin cuenta).  
Ningún sistema es 100 % seguro: mantené tu equipo actualizado y protegé los archivos sensibles que transcribás.

---

## 7. Menores

La Aplicación no está dirigida a recopilar datos de menores. Si un menor la usa, debe ser con supervisión de un adulto responsable según la ley local.

---

## 8. Cambios

Podemos actualizar esta Política. La versión vigente estará en el repositorio.  
Los cambios materiales se reflejarán con una nueva fecha de “Última actualización”.

---

## 9. Contacto

Consultas de privacidad: *issue* en  
https://github.com/Juaandress/whisper-transcriber/issues

---

## Documentos relacionados

- [Términos y Condiciones](TERMINOS.md)
- [Licencia MIT](../LICENSE)
