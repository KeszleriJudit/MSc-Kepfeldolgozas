import cv2
import os
import numpy
import sys
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'


if __name__ == '__main__':

    print("Hello World!")
    counter = sys.argv[1]
    print(counter)
    pathImage = "C:/Users/Judit/Desktop/{}-sudoku.png".format(counter)
    print(pathImage)
    assert os.path.exists(pathImage)
    
    img = cv2.imread(pathImage)
    height, width, c = img.shape
    imgBlank = numpy.zeros((height, width, 3), numpy.uint8)

    imgGray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    imgBlur = cv2.GaussianBlur(imgGray, (5, 5), 1)
    imgThreshold = cv2.adaptiveThreshold(imgBlur, 255, 1, 1, 11, 2)

    imgContours = img.copy()
    imgBigContour = img.copy()
    contours, hierarchy = cv2.findContours(imgThreshold, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(imgContours, contours, -1, (0, 255, 0), 3)

    biggest = numpy.array([])
    maxArea = 0
    for i in contours:
        area = cv2.contourArea(i)
        if area > 50:
            peri = cv2.arcLength(i, True)
            approx = cv2.approxPolyDP(i, 0.02 * peri, True)
            if area > maxArea and len(approx) == 4:
                biggest = approx
                maxArea = area

    if biggest.size != 0:
        myPoints = biggest.reshape((4, 2))
        myPointsNew = numpy.zeros((4, 1, 2), dtype=numpy.int32)
        add = myPoints.sum(1)
        myPointsNew[0] = myPoints[numpy.argmin(add)]
        myPointsNew[3] =myPoints[numpy.argmax(add)]
        diff = numpy.diff(myPoints, axis=1)
        myPointsNew[1] =myPoints[numpy.argmin(diff)]
        myPointsNew[2] = myPoints[numpy.argmax(diff)]
        biggest = myPointsNew

        cv2.drawContours(imgBigContour, biggest, -1, (0, 0, 255), 25)
        pts1 = numpy.float32(biggest)
        pts2 = numpy.float32([[0, 0],[width, 0], [0, height],[width, height]])
        matrix = cv2.getPerspectiveTransform(pts1, pts2)
        imgWarpColored = cv2.warpPerspective(img, matrix, (width, height))
        imgDetectedDigits = imgBlank.copy()
        imgWarpColored = cv2.cvtColor(imgWarpColored,cv2.COLOR_BGR2GRAY)

    cv2.imwrite("C:/Users/Judit/Desktop/{}-sudoku-processed.png".format(counter), imgWarpColored)

    print("Finish")
