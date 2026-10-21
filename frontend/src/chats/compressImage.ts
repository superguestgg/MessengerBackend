import { formatSize } from '../format'

// GIF is left out: a canvas keeps only its first frame.
const compressibleTypes = new Set(['image/jpeg', 'image/png', 'image/webp'])
const maxSide = 2560
const qualities = [0.85, 0.75, 0.6, 0.45]
const attempts = 4

export function isCompressibleImage(file: File) {
  return compressibleTypes.has(file.type)
}

function toJpeg(canvas: HTMLCanvasElement, quality: number): Promise<Blob> {
  return new Promise((resolve, reject) =>
    canvas.toBlob(
      (blob) => (blob != null ? resolve(blob) : reject(new Error('Не удалось сжать изображение'))),
      'image/jpeg',
      quality,
    ),
  )
}

function jpegName(name: string) {
  const dot = name.lastIndexOf('.')
  return `${dot > 0 ? name.slice(0, dot) : name}.jpg`
}

// The server keeps its size limit; a photo over it is re-encoded as JPEG, smaller each try, until it fits.
export async function compressImage(file: File, maxBytes: number): Promise<File> {
  const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
  try {
    let scale = Math.min(1, maxSide / Math.max(bitmap.width, bitmap.height))
    for (let attempt = 0; attempt < attempts; attempt++) {
      const canvas = document.createElement('canvas')
      canvas.width = Math.max(1, Math.round(bitmap.width * scale))
      canvas.height = Math.max(1, Math.round(bitmap.height * scale))
      const context = canvas.getContext('2d')
      if (context == null) throw new Error('Не удалось сжать изображение')
      // JPEG has no transparency: without a background transparent PNG areas turn black.
      context.fillStyle = '#fff'
      context.fillRect(0, 0, canvas.width, canvas.height)
      context.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
      for (const quality of qualities) {
        const blob = await toJpeg(canvas, quality)
        if (blob.size <= maxBytes) return new File([blob], jpegName(file.name), { type: 'image/jpeg' })
      }
      scale *= 0.7
    }
  } finally {
    bitmap.close()
  }
  throw new Error(`не сжимается до ${formatSize(maxBytes)}`)
}
