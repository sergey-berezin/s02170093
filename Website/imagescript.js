let image = document.getElementById('image')
let imgArray = new Array()
let cur = 0

$(async () => {
    try {
        $(document).ready(async function () {
            $('#radio').change(async function () {
                let selectedVal = ""
                let selected = $("input[type='radio'][name='label']:checked")
                if (selected.length > 0) {
                    selectedVal = selected.val()
                    $('#prev').show()
                    $('#next').show()
                    $('#image').show()
                }

                let response = await fetch('https://localhost:44353/rec/' + (selectedVal.split(' '))[0])
                let text = await response.text()

                let testImages = text.split(',')

                imgArray = new Array()

                for (let i = 0; i < testImages.length; i++) {
                    const byteCharacters = atob(testImages[i])

                    const byteNumbers = new Array(byteCharacters.length)
                    for (let i = 0; i < byteCharacters.length; i++) {
                        byteNumbers[i] = byteCharacters.charCodeAt(i)
                    }

                    const byteArray = new Uint8Array(byteNumbers)

                    const blob = new Blob([byteArray])

                    const objectURL = URL.createObjectURL(blob)

                    imgArray.push(objectURL)
                }

                image.src = imgArray[0]
                cur = 0
            })
        })
    }

    catch (ex) {
        console.log(ex)
    }
})

function prev() {
    --cur
    if (cur < 0)
        cur = imgArray.length - 1

    image.src = imgArray[cur]
}

function next() {
    ++cur
    if (cur > imgArray.length - 1)
        cur = 0

    image.src = imgArray[cur]
}